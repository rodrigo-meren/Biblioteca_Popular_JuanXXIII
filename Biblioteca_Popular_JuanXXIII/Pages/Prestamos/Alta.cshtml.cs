using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages.Prestamos
{
    public class AltaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AltaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? LibroId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SocioId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ReservaId { get; set; }

        [BindProperty]
        public Prestamo Prestamo { get; set; } = new Prestamo();

        public async Task<IActionResult> OnGetAsync(int? libroId, int? socioId, int? reservaId)
        {
            CargarListasDesplegables();

            if (libroId.HasValue && socioId.HasValue)
            {
                Prestamo.LibroId = libroId.Value;
                Prestamo.SocioId = socioId.Value;

                // Asignamos la fecha de devolución predeterminada (7 días)
                Prestamo.FechaDevolucion = DateTime.Now.AddDays(7);

              
                var libroPreseleccionado = await _context.Libros.FindAsync(libroId.Value);
                if (libroPreseleccionado != null)
                {
                    ViewData["LibroPreseleccionadoTexto"] = $"{libroPreseleccionado.CodigoInventario} - {libroPreseleccionado.Titulo} ({libroPreseleccionado.Autor})";
                }
            }

            return Page();
        }

        // --- BUSCADOR EN VIVO DE SELECT2 PARA LIBROS ---
        public async Task<JsonResult> OnGetBuscarLibrosAsync(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return new JsonResult(new { results = new List<object>() });
            }

            // 1. Pasamos lo que escribió el usuario a minúsculas
            var terminoBusqueda = term.ToLower();

            // 2. Filtramos pasando también los campos de la BD a minúsculas
            var libros = await _context.Libros
                .Where(l => l.Titulo.ToLower().Contains(terminoBusqueda) ||
                            l.Autor.ToLower().Contains(terminoBusqueda) ||
                            l.CodigoInventario.ToLower().Contains(terminoBusqueda)) // Si inventario es string
                .Select(l => new
                {
                    id = l.Id,
                    text = $"{l.CodigoInventario} - {l.Titulo} ({l.Autor})"
                })
                .Take(15) // Limitamos a 15 para que no explote
                .ToListAsync();

            return new JsonResult(new { results = libros });
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
                // Si el libro NO está disponible, pero venimos procesando una Reserva válida (ReservaId > 0),
                // dejamos continuar porque el libro estaba bloqueado justamente para esta entrega.
                if (!libro.Disponible && (!ReservaId.HasValue || ReservaId.Value <= 0))
                {
                    ModelState.AddModelError("Prestamo.LibroId", "¡Atención! Este libro se encuentra prestado actualmente y no está disponible para un nuevo retiro.");
                    CargarListasDesplegables();
                    return Page();
                }

                // El libro pasa a estar prestado
                libro.Disponible = false;
            }

            // 1. Guardamos el Préstamo
            Prestamo.FechaPrestamo = DateTime.Now;
            Prestamo.Estado = "Activo";
            _context.Prestamos.Add(Prestamo);

            // 2. SI VENÍA DE UNA RESERVA: La marcamos como completada
            if (ReservaId.HasValue && ReservaId.Value > 0)
            {
                var reserva = await _context.Reservas.FindAsync(ReservaId.Value);
                if (reserva != null)
                {
                    reserva.Estado = "Completada";
                }
            }
            var registroAuditoria = new Models.Auditoria
            {
                UsuarioResponsable = User.Identity.Name, // Guarda el DNI del admin logueado
                Modulo = "Prestamos",
                Accion = "Alta de Prestamo",
                Detalles = $"Se dio de alta el préstamo: {Prestamo.Libro.Titulo} ({Prestamo.Libro.CodigoInventario})."
            };
            _context.Auditorias.Add(registroAuditoria);

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = "Préstamo registrado correctamente.";
            return RedirectToPage("./Inicio");
        }

        private void CargarListasDesplegables()
        {
            // Carga normal para la lista de Socies
            var listaSocies = _context.Socios.Select(s => new
            {
                Id = s.Id,
                Descripcion = s.Apellido + ", " + s.Nombre + " (DNI: " + s.DniCarnet + ")"
            }).OrderBy(s => s.Descripcion).ToList();

            ViewData["SocioId"] = new SelectList(listaSocies, "Id", "Descripcion");
        }
    }
}