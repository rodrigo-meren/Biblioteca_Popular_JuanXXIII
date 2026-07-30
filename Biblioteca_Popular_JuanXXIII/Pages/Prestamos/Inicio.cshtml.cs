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
                var libro = await _context.Libros.FindAsync(prestamo.LibroId);

                // 2. Buscamos si hay alguien en cola (En Espera) para este mismo libro.
                // Usamos OrderBy para darle prioridad al que lo reservó primero.
                var reservaPendiente = await _context.Reservas
                    .Where(r => r.LibroId == libro.Id && r.Estado == "En Espera")
                    .OrderBy(r => r.FechaSolicitud)
                    .FirstOrDefaultAsync();

                if (reservaPendiente != null)
                {
                    // ¡Hay alguien esperando! El libro NO vuelve al catálogo público.
                    libro.Disponible = false;

                    // Le cambiamos el estado a la reserva y le damos 48hs de límite para buscarlo
                    reservaPendiente.Estado = "Pendiente de retiro";
                    reservaPendiente.FechaLimiteRetiro = DateTime.Now.AddDays(2);
                }
                else
                {
                    // Nadie lo reservó, el libro vuelve a la estantería normal
                    libro.Disponible = true;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Inicio");
        }
    }
}