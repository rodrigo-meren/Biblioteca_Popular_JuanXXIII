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
            string mesActual = DateTime.Now.ToString("MMMM yyyy", new CultureInfo("es-AR"));
            decimal montoFijo = 1500.00m;

            var sociesActivos = await _context.Socios.ToListAsync();

            foreach (var socio in sociesActivos)
            {
                bool yaTieneCuota = await _context.Cuotas
                    .AnyAsync(c => c.SocioId == socio.Id && c.MesCorrespondiente == mesActual);

                if (!yaTieneCuota)
                {
                    var nuevaCuota = new Cuota
                    {
                        SocioId = socio.Id,
                        Monto = montoFijo,
                        MesCorrespondiente = mesActual,
                        FechaVencimiento = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 10),
                        Estado = "Pendiente"
                    };
                    _context.Cuotas.Add(nuevaCuota);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./Inicio");
        }
    }
}