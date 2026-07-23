using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Biblioteca_Popular_JuanXXIII.Pages.Prestamos
{
    public class AltaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AltaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Acá Prestamo es UN SOLO objeto, por eso funciona el LibroId
        [BindProperty]
        public Prestamo Prestamo { get; set; } = default!;

        public IActionResult OnGet()
        {
            CargarListasDesplegables();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                CargarListasDesplegables();
                return Page();
            }

            var libro = await _context.Libros.FindAsync(Prestamo.LibroId);

            if (libro != null)
            {
                if (libro.Disponible == false)
                {
                    ModelState.AddModelError("Prestamo.LibroId", "¡Atención! Este libro se encuentra prestado actualmente y no está disponible para un nuevo retiro.");
                    CargarListasDesplegables();
                    return Page();
                }

                libro.Disponible = false;
            }

            _context.Prestamos.Add(Prestamo);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Inicio");
        }

        private void CargarListasDesplegables()
        {
            var listaSocies = _context.Socios.Select(s => new
            {
                Id = s.Id,
                Descripcion = s.Apellido + ", " + s.Nombre + " (DNI: " + s.DniCarnet + ")"
            }).OrderBy(s => s.Descripcion).ToList();

            ViewData["SocioId"] = new SelectList(listaSocies, "Id", "Descripcion");

            var listaLibros = _context.Libros.Select(l => new
            {
                Id = l.Id,
                Descripcion = l.Titulo + " - " + l.Autor
            }).OrderBy(l => l.Descripcion).ToList();

            ViewData["LibroId"] = new SelectList(listaLibros, "Id", "Descripcion");
        }
    }
}