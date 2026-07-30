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

            return RedirectToPage(new { id = libroId });
        }
    }
}