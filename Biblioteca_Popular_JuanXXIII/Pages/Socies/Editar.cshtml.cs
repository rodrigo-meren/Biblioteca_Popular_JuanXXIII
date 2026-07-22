using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblioteca_Popular_JuanXXIII;
using Biblioteca_Popular_JuanXXIII.Data;

namespace Biblioteca_Popular_JuanXXIII.Pages.Socies;

public class EditarModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditarModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Socio Socio { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var socio = await _context.Socios.FirstOrDefaultAsync(m => m.Id == id);
        if (socio is null)
        {
            return NotFound();
        }
        Socio = socio;
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

        _context.Attach(Socio).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SocioExists(Socio.Id))
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

    private bool SocioExists(int id)
    {
        return _context.Socios.Any(e => e.Id == id);
    }
}
