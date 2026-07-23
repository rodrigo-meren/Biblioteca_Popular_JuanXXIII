using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models; // O la carpeta donde tengas tus modelos
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages.Prestamos
{
    public class DetallesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetallesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Prestamo Prestamo { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // Incluimos TODO el arbol de datos: Préstamo -> Socio -> Libro
            var prestamo = await _context.Prestamos
                .Include(p => p.Socio)
                .Include(p => p.Libro)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (prestamo == null) return NotFound();

            Prestamo = prestamo;
            return Page();
        }
    }
}