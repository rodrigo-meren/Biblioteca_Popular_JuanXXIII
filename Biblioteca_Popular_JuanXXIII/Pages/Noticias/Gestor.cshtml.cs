using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages.Noticias
{
    // Solo los administradores pueden entrar a gestionar noticias
    [Authorize(Roles = "Administrador")]
    public class GestorModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GestorModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Noticia> ListaNoticias { get; set; } = new List<Noticia>();

        [BindProperty]
        public Noticia NuevaNoticia { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            // Traemos todas las noticias, de la más nueva a la más vieja
            ListaNoticias = await _context.Noticias
                .OrderByDescending(n => n.FechaPublicacion)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCrearAsync()
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage();
            }

            NuevaNoticia.FechaPublicacion = DateTime.Now;
            NuevaNoticia.Activa = true;

            _context.Noticias.Add(NuevaNoticia);
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = "¡La noticia se publicó correctamente en la portada!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAlternarEstadoAsync(int id)
        {
            var noticia = await _context.Noticias.FindAsync(id);
            if (noticia != null)
            {
                // Si estaba activa la apaga, si estaba apagada la prende
                noticia.Activa = !noticia.Activa;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            var noticia = await _context.Noticias.FindAsync(id);
            if (noticia != null)
            {
                _context.Noticias.Remove(noticia);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = "Noticia eliminada definitivamente.";
            }

            return RedirectToPage();
        }
    }
}