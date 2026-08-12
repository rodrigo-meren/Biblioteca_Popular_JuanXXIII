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
}