using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Biblioteca_Popular_JuanXXIII.Pages.Cuotas
{
    [Authorize(Roles = "Administrador, Bibliotecario")]
    public class InicioModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public InicioModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Variables para las estadísticas
        public decimal RecaudacionDelMes { get; set; }
        public decimal DeudaPendiente { get; set; }
        public int CantidadPagosMes { get; set; }
        public int CantidadMorosos { get; set; }

        // Lista para ver los últimos pagos que entraron
        public IList<Cuota> UltimosPagos { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // 1. Calculamos la plata que entró este mes
            RecaudacionDelMes = await _context.Cuotas
                .Where(c => c.Estado == "Pagada" && c.FechaPago >= inicioMes)
                .SumAsync(c => c.Monto);

            // 2. Calculamos cuántos pagos se hicieron este mes
            CantidadPagosMes = await _context.Cuotas
                .Where(c => c.Estado == "Pagada" && c.FechaPago >= inicioMes)
                .CountAsync();

            // 3. Calculamos la deuda total en la calle
            DeudaPendiente = await _context.Cuotas
                .Where(c => c.Estado == "Pendiente" || c.Estado == "Vencida")
                .SumAsync(c => c.Monto);

            // 4. Calculamos cuántas cuotas están sin pagar
            CantidadMorosos = await _context.Cuotas
                .Where(c => c.Estado == "Pendiente" || c.Estado == "Vencida")
                .CountAsync();

            // 5. Traemos los últimos 10 pagos registrados para el historial
            UltimosPagos = await _context.Cuotas
                .Include(c => c.Socio)
                .Where(c => c.Estado == "Pagada")
                .OrderByDescending(c => c.FechaPago)
                .Take(10)
                .ToListAsync();
        }

        // Mantenemos el método para generar el lote masivo
        public async Task<IActionResult> OnPostGenerarLoteAsync()
        {
            decimal montoFijo = 3000.00m;
            var fechaActual = DateTime.Now;

            // Traemos solo a los socios activos
            var sociesActivos = await _context.Socios
                .Where(s => s.Activo == true)
                .ToListAsync();

            foreach (var socio in sociesActivos)
            {
                // Si no pagan cuota, los saltamos directamente
                if (socio.Categoria == "Honorario" || socio.Categoria == "Vitalicio")
                    continue;

                // Tomamos la fecha de vencimiento del socio. 
                // Si por alguna razón está vacía (null), usamos el día 10 del mes actual para que no explote.
                DateTime fechaIteracion = socio.FechaVencimiento ?? new DateTime(fechaActual.Year, fechaActual.Month, 10);

                // BUCLE: Mientras el mes y año evaluado sea menor o igual al mes y año actual
                // (Convertimos a día 1 para comparar solo meses y años enteros, sin importar el día)
                while (new DateTime(fechaIteracion.Year, fechaIteracion.Month, 1) <= new DateTime(fechaActual.Year, fechaActual.Month, 1))
                {
                    // Nombre del mes para esta vuelta (ej: "junio 2026", "julio 2026")
                    string mesCorrespondiente = fechaIteracion.ToString("MMMM yyyy", new CultureInfo("es-AR"));

                    // Nos fijamos si YA le cobramos este mes específico a este socio
                    bool yaTieneCuota = await _context.Cuotas
                        .AnyAsync(c => c.SocioId == socio.Id && c.MesCorrespondiente == mesCorrespondiente);

                    if (!yaTieneCuota)
                    {
                        var nuevaCuota = new Cuota
                        {
                            SocioId = socio.Id,
                            Monto = montoFijo,
                            MesCorrespondiente = mesCorrespondiente,
                            FechaVencimiento = fechaIteracion,
                            // Si la fecha de la cuota ya pasó, la creamos directamente como "Vencida"
                            Estado = fechaIteracion.Date < fechaActual.Date ? "Vencida" : "Pendiente"
                        };

                        _context.Cuotas.Add(nuevaCuota);
                    }

                    // Sumamos un mes a la fecha para la próxima vuelta del bucle
                    fechaIteracion = fechaIteracion.AddMonths(1);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./Inicio");
        }

        public async Task<IActionResult> OnPostResetearCeroAsync()
        {
            // Buscamos todas las cuotas que se hayan generado y estén pendientes o vencidas
            var cuotasPendientes = await _context.Cuotas
                .Where(c => c.Estado == "Pendiente" || c.Estado == "Vencida")
                .ToListAsync();

            // Las eliminamos masivamente. Esto deja la deuda total en $0 
            // y libera a los socios para que el bibliotecario los pueda borrar/limpiar tranquilo.
            _context.Cuotas.RemoveRange(cuotasPendientes);

            await _context.SaveChangesAsync();

            return RedirectToPage("./Inicio");
        }
    }
}