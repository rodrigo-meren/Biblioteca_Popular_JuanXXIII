
using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MercadoPago.Config;
using MercadoPago.Client.Preference;

namespace Biblioteca_Popular_JuanXXIII.Pages.MiPerfil
{


    [Authorize(Roles = "Socio, Administrador")]
    public class InicioModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InicioModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Socio SocioActual { get; set; } = default!;
        public IList<Prestamo> MisPrestamos { get; set; } = new List<Prestamo>();
        public IList<Cuota> MisCuotasPendientes { get; set; } = new List<Cuota>();

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Obtenemos el ID de la cuenta de Identity que acaba de loguearse
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Buscamos su ficha real de Socio usando ese ID
            SocioActual = await _context.Socios
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            if (SocioActual == null)
            {
                return RedirectToPage("/Account/Logout", new { area = "Identity" });
            }

            // 3. Traemos sus libros que AÚN no devolvió (FechaDevolucion es null)
            MisPrestamos = await _context.Prestamos
                .Include(p => p.Libro)
                .Where(p => p.SocioId == SocioActual.Id && p.FechaDevolucion == null)
                .OrderBy(p => p.FechaVencimiento)
                .ToListAsync();
            MisReservas = await _context.Reservas
                .Include(r => r.Libro)
                .Where(r => r.SocioId == SocioActual.Id && (r.Estado == "En Espera" || r.Estado == "Pendiente de retiro"))
                .OrderBy(r => r.FechaSolicitud)
                .ToListAsync();

            // 4. Traemos sus deudas
            MisCuotasPendientes = await _context.Cuotas
                .Where(c => c.SocioId == SocioActual.Id && (c.Estado == "Pendiente" || c.Estado == "Vencida"))
                .OrderBy(c => c.FechaVencimiento)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSolicitarProrrogaAsync(int prestamoId)
        {
            // 1. Buscamos el ID del usuario logueado por seguridad
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var socio = await _context.Socios.FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            if (socio == null) return RedirectToPage("/Account/Logout", new { area = "Identity" });

            // 2. Buscamos el préstamo específico que quiere extender
            var prestamo = await _context.Prestamos
                .FirstOrDefaultAsync(p => p.Id == prestamoId && p.SocioId == socio.Id);

            // 3. Verificamos que exista y que NO esté vencido
            if (prestamo != null && prestamo.FechaVencimiento >= DateTime.Now)
            {
                // Le sumamos 7 días de gracia a la fecha original
                prestamo.FechaVencimiento = prestamo.FechaVencimiento.AddDays(7);
                await _context.SaveChangesAsync();
            }

            // Recargamos la página para que vea la nueva fecha
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostPagarConMercadoPagoAsync()
        {
            // 1. Identificamos al socio
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var socio = await _context.Socios.FirstOrDefaultAsync(s => s.IdentityUserId == userId);
            if (socio == null) return RedirectToPage("/Account/Logout", new { area = "Identity" });

            // 2. Calculamos la deuda total exacta
            var cuotasPendientes = await _context.Cuotas
                .Where(c => c.SocioId == socio.Id && (c.Estado == "Pendiente" || c.Estado == "Vencida"))
                .ToListAsync();

            decimal deudaTotal = cuotasPendientes.Sum(c => c.Monto);
            if (deudaTotal <= 0) return RedirectToPage(); // Si no debe nada, recarga la página

            // 3. Credenciales de Mercado Pago (El Access Token)
            MercadoPago.Config.MercadoPagoConfig.AccessToken = "TEST-ACA_VA_TU_TOKEN_DE_PRUEBA";

            // 4. Armamos el "carrito" de cobro
            var request = new MercadoPago.Client.Preference.PreferenceRequest
            {
                Items = new List<MercadoPago.Client.Preference.PreferenceItemRequest>
                {
                    new MercadoPago.Client.Preference.PreferenceItemRequest
                    {
                        Title = "Cuotas atrasadas - Biblioteca Juan XXIII",
                        Quantity = 1,
                        CurrencyId = "ARS",
                        UnitPrice = deudaTotal
                    }
                },
                BackUrls = new MercadoPago.Client.Preference.PreferenceBackUrlsRequest
                {
                    // Como vi en tus fotos que usás el puerto 7114, te dejo las rutas exactas
                    Success = "https://localhost:7114/MiPerfil/Inicio?pago=exito",
                    Failure = "https://localhost:7114/MiPerfil/Inicio?pago=error",
                    Pending = "https://localhost:7114/MiPerfil/Inicio?pago=pendiente"
                },
                AutoReturn = "approved" // Si paga bien, lo devuelve a tu página automáticamente
            };

            // 5. Creamos el link y mandamos al socie para allá
            var client = new MercadoPago.Client.Preference.PreferenceClient();
            MercadoPago.Resource.Preference.Preference preference = await client.CreateAsync(request);

            return Redirect(preference.InitPoint);
        }
        public IList<Reserva> MisReservas { get; set; } = new List<Reserva>();
    }

}