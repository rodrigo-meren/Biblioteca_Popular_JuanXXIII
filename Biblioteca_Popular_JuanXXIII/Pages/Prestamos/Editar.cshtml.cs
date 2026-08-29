using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages.Prestamos
{
    public class EditarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Prestamo Prestamo { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // Traemos el préstamo con sus relaciones para que no falle nada en la vista
            var prestamo = await _context.Prestamos
                .Include(p => p.Socio)
                .Include(p => p.Libro)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (prestamo == null) return NotFound();

            Prestamo = prestamo;
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
            var registroAuditoria = new Models.Auditoria
            {
                UsuarioResponsable = User.Identity.Name, // Guarda el DNI del admin logueado
                Modulo = "Prestamos",
                Accion = "Edición de Préstamo",
                Detalles = $"Se editó el préstamo: {Prestamo.Libro.Titulo} ({Prestamo.Libro.CodigoInventario})."
            };
            _context.Auditorias.Add(registroAuditoria);

            // Le decimos a Entity Framework que actualice todo el registro modificado
            _context.Attach(Prestamo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PrestamoExists(Prestamo.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Inicio");
        }

        private bool PrestamoExists(int id) => _context.Prestamos.Any(e => e.Id == id);

        // El mismo método auxiliar que usamos en el Alta
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