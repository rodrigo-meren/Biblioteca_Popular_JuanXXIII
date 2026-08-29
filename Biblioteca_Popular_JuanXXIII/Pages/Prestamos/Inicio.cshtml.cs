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

        // --- NUEVA PROPIEDAD PARA EL BUSCADOR ---
        [BindProperty(SupportsGet = true)]
        public string BuscarTexto { get; set; }

        // --- PROPIEDADES PARA PAGINACIÓN ---
        [BindProperty(SupportsGet = true)]
        public int PaginaActual { get; set; } = 1;
        public int TotalPaginas { get; set; }
        public int RegistrosPorPagina { get; set; } = 15;

        public async Task OnGetAsync()
        {
            if (_context.Prestamos != null)
            {
                // 1. Empezamos la consulta excluyendo los ya devueltos
                IQueryable<Prestamo> consulta = _context.Prestamos
                    .Include(p => p.Socio)
                    .Include(p => p.Libro)
                    .Where(p => p.Estado != "Devuelto");

                // 2. Filtro del Buscador (Título del libro o Nombre/Apellido del socio)
                if (!string.IsNullOrEmpty(BuscarTexto))
                {
                    var termino = BuscarTexto.ToLower();
                    consulta = consulta.Where(p =>
                        p.Libro.Titulo.ToLower().Contains(termino) ||
                        p.Socio.Nombre.ToLower().Contains(termino) ||
                        p.Socio.Apellido.ToLower().Contains(termino));
                }

                // 3. Ordenamos por prioridad de devolución (los más próximos a vencer o vencidos van primero)
                consulta = consulta.OrderBy(p => p.FechaVencimiento);

                // 4. Lógica matemática de la Paginación
                var cantidadTotal = await consulta.CountAsync();
                TotalPaginas = (int)Math.Ceiling(cantidadTotal / (double)RegistrosPorPagina);

                if (PaginaActual < 1) PaginaActual = 1;
                if (PaginaActual > TotalPaginas && TotalPaginas > 0) PaginaActual = TotalPaginas;

                // 5. Ejecutamos la consulta con el Skip y Take
                Prestamo = await consulta
                    .Skip((PaginaActual - 1) * RegistrosPorPagina)
                    .Take(RegistrosPorPagina)
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
                var libro = await _context.Libros.FindAsync(prestamo.LibroId);

                var reservaPendiente = await _context.Reservas
                    .Where(r => r.LibroId == libro.Id && r.Estado == "En Espera")
                    .OrderBy(r => r.FechaSolicitud)
                    .FirstOrDefaultAsync();

                if (reservaPendiente != null)
                {
                    libro.Disponible = false;
                    reservaPendiente.Estado = "Pendiente de retiro";
                    reservaPendiente.FechaLimiteRetiro = DateTime.Now.AddDays(2);
                }
                else
                {
                    libro.Disponible = true;
                }
                

                await _context.SaveChangesAsync();
            }
            var registroAuditoria = new Models.Auditoria
            {
                UsuarioResponsable = User.Identity.Name, // Guarda el DNI del admin logueado
                Modulo = "Prestamos",
                Accion = "Devolución de Préstamo",
                Detalles = $"Se devolvió el préstamo: {prestamo.Libro.Titulo} ({prestamo.Libro.CodigoInventario})."
            };
            _context.Auditorias.Add(registroAuditoria);
            return RedirectToPage("./Inicio");
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            var prestamo = await _context.Prestamos.FindAsync(id);

            if (prestamo != null)
            {
                if (prestamo.Estado != "Devuelto")
                {
                    var libro = await _context.Libros.FindAsync(prestamo.LibroId);
                    if (libro != null)
                    {
                        libro.Disponible = true;
                    }
                }

                _context.Prestamos.Remove(prestamo);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Inicio");
        }
    }
}