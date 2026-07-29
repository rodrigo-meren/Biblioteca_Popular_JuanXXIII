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

    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see https://aka.ms/RazorPagesCRUD.
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

        return RedirectToPage("./Inicio");
    }

    private bool LibroExists(int id)
    {
        return _context.Libros.Any(e => e.Id == id);
    }
}
