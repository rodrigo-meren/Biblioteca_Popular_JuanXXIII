using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblioteca_Popular_JuanXXIII.Models;
using Biblioteca_Popular_JuanXXIII.Data;

namespace Biblioteca_Popular_JuanXXIII.Pages.Libros;

public class InicioModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public InicioModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Libro> Libro { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Libro = await _context.Libros.ToListAsync();
    }
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        // Buscamos el libro en la base de datos
        var libro = await _context.Libros.FindAsync(id);

        if (libro != null)
        {
            // Lo removemos y guardamos los cambios
            _context.Libros.Remove(libro);
            await _context.SaveChangesAsync();
        }

        // Recargamos la misma página para que la grilla se actualice sola
        return RedirectToPage("./Inicio");
    }
}
