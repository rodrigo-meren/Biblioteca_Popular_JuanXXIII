
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

        public async Task<IActionResult> OnGetAsync(string pago, int meses = 0)
        {
            // 1. Obtenemos el ID del usuario
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            SocioActual = await _context.Socios.FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            if (SocioActual == null) return RedirectToPage("/Account/Logout", new { area = "Identity" });

            // ==========================================
            // LÓGICA DE INTERCEPCIÓN DE MERCADO PAGO
            // ==========================================
            if (pago == "exito" && meses > 0)
            {
                await ProcesarPagoExitosoAsync(SocioActual.Id, meses);
                TempData["Mensaje"] = $"¡Pago procesado con éxito! Se acreditaron {meses} mes(es) a tu cuenta.";

                // Redirigimos a la misma página limpia para borrar el "?pago=exito" de la URL
                return RedirectToPage();
            }
            else if (pago == "error")
            {
                TempData["MensajeError"] = "Hubo un problema procesando tu pago. Por favor, intentá de nuevo.";
            }

            // 2. Traemos sus libros, reservas y deudas (Tu código original)
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

        public async Task<IActionResult> OnPostPagarConMercadoPagoAsync(int cantidadMeses)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var socio = await _context.Socios.FirstOrDefaultAsync(s => s.IdentityUserId == userId);
            if (socio == null) return RedirectToPage("/Account/Logout", new { area = "Identity" });

            // Calculamos el total (Fijamos la cuota en 2000, cambialo si el valor es otro)
            decimal valorCuota = 2000m;
            decimal totalAPagar = cantidadMeses * valorCuota;

            // Credenciales de Mercado Pago (Reemplazar por el tuyo de Producción cuando esté listo)
            MercadoPago.Config.MercadoPagoConfig.AccessToken = "TEST-ACA_VA_TU_TOKEN_DE_PRUEBA";

            var request = new MercadoPago.Client.Preference.PreferenceRequest
            {
                Items = new List<MercadoPago.Client.Preference.PreferenceItemRequest>
                {
                    new MercadoPago.Client.Preference.PreferenceItemRequest
                    {
                        Title = $"Biblioteca Juan XXIII - {cantidadMeses} cuota(s)",
                        Quantity = 1,
                        CurrencyId = "ARS",
                        UnitPrice = totalAPagar
                    }
                },
                BackUrls = new MercadoPago.Client.Preference.PreferenceBackUrlsRequest
                {
                    // Le pasamos a la URL de éxito cuántos meses pagó para que el OnGet los procese
                    Success = $"https://localhost:7114/MiPerfil/Inicio?pago=exito&meses={cantidadMeses}",
                    Failure = "https://localhost:7114/MiPerfil/Inicio?pago=error",
                    Pending = "https://localhost:7114/MiPerfil/Inicio?pago=pendiente"
                },
                AutoReturn = "approved"
            };

            var client = new MercadoPago.Client.Preference.PreferenceClient();
            MercadoPago.Resource.Preference.Preference preference = await client.CreateAsync(request);

            return Redirect(preference.InitPoint);
        }

        // ==========================================
        // EL MOTOR DE ACREDITACIÓN DE CUOTAS
        // ==========================================
        private async Task ProcesarPagoExitosoAsync(int socioId, int mesesPagados)
        {
            int mesesRestantes = mesesPagados;

            // 1. Primero saldamos las cuotas viejas que estaban pendientes
            // Usamos FechaVencimiento para ordenarlas de la más antigua a la más nueva
            var cuotasAtrasadas = await _context.Cuotas
                .Where(c => c.SocioId == socioId && (c.Estado == "Pendiente" || c.Estado == "Vencida"))
                .OrderBy(c => c.FechaVencimiento)
                .ToListAsync();

            foreach (var cuota in cuotasAtrasadas)
            {
                if (mesesRestantes > 0)
                {
                    cuota.Estado = "Pagada";
                    cuota.FechaPago = DateTime.Now;
                    mesesRestantes--;
                }
            }

            // 2. Si todavía le sobran meses a favor, le creamos cuotas nuevas adelantadas
            if (mesesRestantes > 0)
            {
                // Buscamos cuál es el último mes que figura en su cuenta (pagado o no)
                var ultimaCuota = await _context.Cuotas
                    .Where(c => c.SocioId == socioId)
                    .OrderByDescending(c => c.FechaVencimiento)
                    .FirstOrDefaultAsync();

                DateTime fechaBase;

                if (ultimaCuota != null)
                {
                    // Si ya tenía cuotas previas, arrancamos a sumar desde el mes siguiente
                    fechaBase = ultimaCuota.FechaVencimiento.AddMonths(1);
                }
                else
                {
                    // Si es un socio completamente nuevo y nunca tuvo cuotas, arrancamos desde el mes actual
                    fechaBase = DateTime.Now;
                }

                // Generamos los meses a favor
                for (int i = 0; i < mesesRestantes; i++)
                {
                    _context.Cuotas.Add(new Cuota
                    {
                        SocioId = socioId,
                        Monto = 2000m, // Podés cambiar este valor por una variable si la cuota aumenta a futuro

                        // Generamos el texto "MM-yyyy" automáticamente (ej: "08-2026")
                        MesCorrespondiente = fechaBase.ToString("MM-yyyy"),

                        // Fijamos el vencimiento el día 10 de cada mes (podés cambiar el 10 por el día que usen en la biblioteca)
                        FechaVencimiento = new DateTime(fechaBase.Year, fechaBase.Month, 10),

                        Estado = "Pagada",
                        FechaPago = DateTime.Now
                    });

                    // Le sumamos un mes a la fecha base para la próxima iteración del bucle
                    fechaBase = fechaBase.AddMonths(1);
                }
            }

            await _context.SaveChangesAsync();
        }
        public IList<Reserva> MisReservas { get; set; } = new List<Reserva>();
    }

}