using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Listas para guardar lo que vamos a mostrar en la portada
        public IList<Libro> LibrosDestacados { get; set; } = new List<Libro>();
        public IList<Noticia> Noticias { get; set; } = new List<Noticia>();

        public async Task OnGetAsync()
        {
            // 1. Buscamos hasta 4 libros que estén marcados como destacados
            LibrosDestacados = await _context.Libros
                .Where(l => l.EsDestacado)
                .Take(4)
                .ToListAsync();

            // 2. Buscamos las últimas 3 noticias activas, de la más nueva a la más vieja
            Noticias = await _context.Noticias
                .Where(n => n.Activa)
                .OrderByDescending(n => n.FechaPublicacion)
                .Take(3)
                .ToListAsync();
        }
    }
}