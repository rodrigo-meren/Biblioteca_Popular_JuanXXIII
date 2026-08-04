using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Biblioteca_Popular_JuanXXIII.Pages.Libros
{
    public class DetallesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetallesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Libro Libro { get; set; } = default!;

        // Usamos tu modelo exacto para la lista
        public IList<Resena> Resenas { get; set; } = new List<Resena>();

        // Propiedades adaptadas a tu clase
        [BindProperty]
        public int NuevaCalificacion { get; set; }

        [BindProperty]
        public string NuevoComentario { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var libro = await _context.Libros.FirstOrDefaultAsync(m => m.Id == id);
            if (libro == null) return NotFound();

            Libro = libro;

            // Traemos las reseñas, pero SOLO las que estén aprobadas (Aprobada == true)
            Resenas = await _context.Resenas
                .Include(r => r.Socio)
                .Where(r => r.LibroId == id && r.Aprobada)
                .OrderByDescending(r => r.FechaPublicacion)
                .ToListAsync();

            return Page();
        }

        // ==========================================
        // 1. MÉTODO PARA DEJAR RESEÑAS
        // ==========================================
        public async Task<IActionResult> OnPostDejarResenaAsync(int libroId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var socio = await _context.Socios.FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            // Verificamos si el socio existe. Tu comentario permite ser nulo, pero si hay texto, lo guardamos.
            if (socio != null)
            {
                var resena = new Resena
                {
                    LibroId = libroId,
                    SocioId = socio.Id,
                    Calificacion = NuevaCalificacion == 0 ? 5 : NuevaCalificacion,
                    Comentario = NuevoComentario,
                    FechaPublicacion = DateTime.Now,
                    Aprobada = true // Nace aprobada para que se vea directo (luego el admin la puede ocultar)
                };

                _context.Resenas.Add(resena);
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = "¡Gracias por compartir tu opinión con la comunidad!";
            }

            // REDIRECCIÓN CORRECTA PARA EVITAR EL ERROR NULL
            return RedirectToPage(new { id = libroId });
        }

        // ==========================================
        // 2. MÉTODO PARA RESERVAR (El que faltaba)
        // ==========================================
        public async Task<IActionResult> OnPostReservarAsync(int libroId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var socio = await _context.Socios.FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            if (socio == null) return RedirectToPage("/Account/Logout", new { area = "Identity" });

            var libro = await _context.Libros.FindAsync(libroId);
            if (libro == null) return NotFound();

            // Verificamos que no haya reservado este mismo libro antes
            var reservaExistente = await _context.Reservas
                .AnyAsync(r => r.LibroId == libroId && r.SocioId == socio.Id && (r.Estado == "Pendiente de retiro" || r.Estado == "En espera"));

            if (reservaExistente)
            {
                TempData["MensajeError"] = "Ya tenés una reserva activa o estás en lista de espera para este libro.";
                // REDIRECCIÓN CORRECTA
                return RedirectToPage(new { id = libroId });
            }

            var nuevaReserva = new Reserva
            {
                LibroId = libro.Id,
                SocioId = socio.Id,
                FechaSolicitud = DateTime.Now
            };

            if (libro.Disponible)
            {
                nuevaReserva.Estado = "Pendiente de retiro";
                nuevaReserva.FechaLimiteRetiro = DateTime.Now.AddHours(48);
                libro.Disponible = false;

                TempData["MensajeExito"] = $"¡Reserva confirmada! Tenés tiempo hasta el {nuevaReserva.FechaLimiteRetiro.Value.ToString("dd/MM HH:mm")} hs para pasar a retirarlo.";
            }
            else
            {
                nuevaReserva.Estado = "En espera";
                nuevaReserva.FechaLimiteRetiro = null;
                TempData["MensajeExito"] = "El libro no está disponible actualmente. Quedaste en la lista de espera y te vamos a avisar cuando lo devuelvan.";
            }

            _context.Reservas.Add(nuevaReserva);
            await _context.SaveChangesAsync();

            // REDIRECCIÓN CORRECTA (Evita el NullReferenceException)
            return RedirectToPage(new { id = libroId });
        }
    }
}