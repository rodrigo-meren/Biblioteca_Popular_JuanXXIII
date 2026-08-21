using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages.Socies;

public class DetallesModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public DetallesModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Socio Socio { get; set; } = default!;
    public IList<Cuota> Cuotas { get; set; } = default!;
    public IList<Prestamo> Prestamos { get; set; } = default!;

    // Variables nuevas para paginar
    public int PaginaActualPrestamos { get; set; } = 1;
    public int TotalPaginasPrestamos { get; set; }
    public int RegistrosPorPaginaPrestamos { get; set; } = 5; // Mostrará 5 libros a la vez

    // Le agregamos el parámetro "paginaPrestamos" al OnGet
    public async Task<IActionResult> OnGetAsync(int? id, int paginaPrestamos = 1)
    {
        if (id == null) return NotFound();

        Socio = await _context.Socios.FirstOrDefaultAsync(m => m.Id == id);
        if (Socio == null) return NotFound();

        // Traemos TODAS las cuotas (suelen ser pocas, no hace falta paginar)
        Cuotas = await _context.Cuotas
            .Where(c => c.SocioId == id)
            .OrderByDescending(c => c.FechaVencimiento)
            .ToListAsync();


        PaginaActualPrestamos = paginaPrestamos;
        if (PaginaActualPrestamos < 1) PaginaActualPrestamos = 1;

        // Armamos la consulta base
        var consultaPrestamos = _context.Prestamos
            .Include(p => p.Libro)
            .Where(p => p.SocioId == id);

        // Contamos cuántos préstamos tiene en total
        var cantidadTotal = await consultaPrestamos.CountAsync();
        TotalPaginasPrestamos = (int)Math.Ceiling(cantidadTotal / (double)RegistrosPorPaginaPrestamos);

        if (PaginaActualPrestamos > TotalPaginasPrestamos && TotalPaginasPrestamos > 0)
            PaginaActualPrestamos = TotalPaginasPrestamos;

        // Ejecutamos la consulta con el Take y el Skip
        Prestamos = await consultaPrestamos
            .OrderByDescending(p => p.FechaPrestamo)
            .Skip((PaginaActualPrestamos - 1) * RegistrosPorPaginaPrestamos)
            .Take(RegistrosPorPaginaPrestamos)
            .ToListAsync();

        return Page();
    }
    // ==========================================
    // DEVOLUCIÓN RÁPIDA DE LIBRO DESDE LA FICHA
    // ==========================================
    public async Task<IActionResult> OnPostDevolverLibroAsync(int prestamoId, int socioId)
    {
        // Buscamos el préstamo y traemos también el libro asociado
        var prestamo = await _context.Prestamos
            .Include(p => p.Libro)
            .FirstOrDefaultAsync(p => p.Id == prestamoId);

        if (prestamo != null && prestamo.Estado != "Devuelto")
        {
            // 1. Actualizamos el préstamo
            prestamo.Estado = "Devuelto";
            prestamo.FechaDevolucion = DateTime.Now;

            // 2. Liberamos el libro para que vuelva al catálogo
            if (prestamo.Libro != null)
            {
                prestamo.Libro.Disponible = true;
            }

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = $"El libro '{prestamo.Libro?.Titulo}' fue devuelto al inventario correctamente.";
        }

        // Recargamos la ficha del socie
        return RedirectToPage(new { id = socioId });
    }
    // ==========================================
    // REGISTRO DE PAGO DE CUOTA EN EFECTIVO
    // ==========================================
    public async Task<IActionResult> OnPostRegistrarPagoAsync(int cuotaId, int socioId)
    {
        // 1. Buscamos la cuota que el bibliotecario quiere cobrar
        var cuota = await _context.Cuotas.FirstOrDefaultAsync(c => c.Id == cuotaId);

        if (cuota != null && cuota.Estado != "Pagada")
        {
            // 2. Marcamos la cuota como pagada y le ponemos la fecha de hoy
            cuota.Estado = "Pagada";
            cuota.FechaPago = DateTime.Now;

            // 3. Buscamos al socio para actualizarle el semáforo de estado
            var socio = await _context.Socios.FirstOrDefaultAsync(s => s.Id == socioId);

            if (socio != null)
            {
                if (socio.FechaVencimiento.HasValue)
                {
                    // Le sumamos un mes a su vencimiento actual
                    socio.FechaVencimiento = socio.FechaVencimiento.Value.AddMonths(1);
                }
                else
                {
                    // Si por algún error no tenía fecha, lo acomodamos para el mes que viene
                    socio.FechaVencimiento = DateTime.Now.AddMonths(1);
                }
            }

            await _context.SaveChangesAsync();

            // Opcional: Mandamos un cartelito verde de éxito a la vista
            TempData["MensajeExito"] = $"El pago de {cuota.MesCorrespondiente} fue registrado con éxito.";
        }

        // Recargamos la ficha del socie para que se actualicen las tablas y el semáforo
        return RedirectToPage(new { id = socioId });
    }
    // ==========================================
    // GENERAR CUOTA ADELANTADA MANUALMENTE
    // ==========================================
    public async Task<IActionResult> OnPostAdelantarMesAsync(int socioId)
    {
        var socio = await _context.Socios.FirstOrDefaultAsync(s => s.Id == socioId);
        if (socio == null) return NotFound();

        // Buscamos cuál es la cuota más lejana en el tiempo que ya tiene generada
        var ultimaCuota = await _context.Cuotas
            .Where(c => c.SocioId == socioId)
            .OrderByDescending(c => c.FechaVencimiento)
            .FirstOrDefaultAsync();

        DateTime fechaBase;

        if (ultimaCuota != null)
        {
            // Si ya tiene cuotas, la nueva será para el mes siguiente a la última
            fechaBase = ultimaCuota.FechaVencimiento.AddMonths(1);
        }
        else if (socio.FechaVencimiento.HasValue)
        {
            // Si no tiene cuotas pero tiene fecha de vencimiento, usamos esa
            fechaBase = socio.FechaVencimiento.Value;
        }
        else
        {
            // Si es un socio nuevo y no tiene nada, usamos el día 10 del mes actual
            fechaBase = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 10);
        }

        // Armamos el nombre del mes (ej: "septiembre 2026")
        string mesCorrespondiente = fechaBase.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-AR"));

        var nuevaCuota = new Cuota
        {
            SocioId = socio.Id,
            Monto = 3000.00m, // Ajustá este valor al precio real de la cuota
            MesCorrespondiente = mesCorrespondiente,
            FechaVencimiento = fechaBase,
            Estado = "Pendiente"
        };

        _context.Cuotas.Add(nuevaCuota);
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = $"Se generó correctamente la cuota adelantada de {mesCorrespondiente}.";

        return RedirectToPage(new { id = socioId });
    }
}