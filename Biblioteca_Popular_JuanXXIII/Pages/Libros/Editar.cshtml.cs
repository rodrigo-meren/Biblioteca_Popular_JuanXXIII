using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblioteca_Popular_JuanXXIII.Models;
using Biblioteca_Popular_JuanXXIII.Data;

namespace Biblioteca_Popular_JuanXXIII.Pages.Libros;

public class EditarModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditarModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Libro Libro { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var libro = await _context.Libros.FirstOrDefaultAsync(m => m.Id == id);
        if (libro is null)
        {
            return NotFound();
        }
        Libro = libro;
        return Page();
    }

 
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Attach(Libro).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!LibroExists(Libro.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        var registroAuditoria = new Models.Auditoria
        {
            UsuarioResponsable = User.Identity.Name, // Guarda el DNI del admin logueado
            Modulo = "Libros",
            Accion = "edicion de Libro",
            Detalles = $"Se editó el libro: {Libro.Titulo} ({Libro.CodigoInventario})."
        };
        _context.Auditorias.Add(registroAuditoria);

        return RedirectToPage("./Inicio");
    }

    private bool LibroExists(int id)
    {
        return _context.Libros.Any(e => e.Id == id);
    }
}
