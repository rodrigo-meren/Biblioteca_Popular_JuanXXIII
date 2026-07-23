using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages.Prestamos
{
    public class InicioModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public InicioModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Prestamo> Prestamo { get; set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.Prestamos != null)
            {
                // Traemos los datos con los Include para armar la tabla
                Prestamo = await _context.Prestamos
                    .Include(p => p.Socio)
                    .Include(p => p.Libro)
                    .OrderByDescending(p => p.FechaPrestamo)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostDevolverAsync(int id)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Libro)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prestamo != null && prestamo.Estado != "Devuelto")
            {
                prestamo.Estado = "Devuelto";
                prestamo.FechaDevolucion = DateTime.Now;

                if (prestamo.Libro != null)
                {
                    prestamo.Libro.Disponible = true;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Inicio");
        }
    }
}