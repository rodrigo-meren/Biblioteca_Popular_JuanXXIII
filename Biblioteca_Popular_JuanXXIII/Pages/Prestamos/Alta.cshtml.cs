using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; // <- Agregamos esto para usar ToListAsync()

namespace Biblioteca_Popular_JuanXXIII.Pages.Prestamos
{
    public class AltaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AltaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Prestamo Prestamo { get; set; } = default!;

        public IActionResult OnGet()
        {
            CargarListasDesplegables();
            return Page();
        }

        // --- NUEVO: MÉTODO PARA EL BUSCADOR EN VIVO DE SELECT2 ---
        public async Task<JsonResult> OnGetBuscarLibrosAsync(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return new JsonResult(new { results = new List<object>() });
            }

            var libros = await _context.Libros
                .Where(l => l.Titulo.Contains(term) || l.Autor.Contains(term))
                .Select(l => new {
                    id = l.Id,
                    // Así se verá en la lista: "Código - Título (Autor)"
                    text = l.CodigoInventario + " - " + l.Titulo + " (" + l.Autor + ")"
                })
                .Take(20) // Limitamos a los mejores 20 resultados para que sea instantáneo
                .ToListAsync();

            return new JsonResult(new { results = libros });
        }
        // ---------------------------------------------------------

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

                libro.Disponible = false; // Lo marcamos como no disponible
            }

            _context.Prestamos.Add(Prestamo);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Inicio");
        }

        private void CargarListasDesplegables()
        {
            // Mantenemos la carga normal para los Socies
            var listaSocies = _context.Socios.Select(s => new
            {
                Id = s.Id,
                Descripcion = s.Apellido + ", " + s.Nombre + " (DNI: " + s.DniCarnet + ")"
            }).OrderBy(s => s.Descripcion).ToList();

            ViewData["SocioId"] = new SelectList(listaSocies, "Id", "Descripcion");

            // ¡Magia! Borramos la carga masiva de Libros de acá.
            // La vista ahora va a usar el método OnGetBuscarLibrosAsync para traerlos de a poco.
        }
    }
}