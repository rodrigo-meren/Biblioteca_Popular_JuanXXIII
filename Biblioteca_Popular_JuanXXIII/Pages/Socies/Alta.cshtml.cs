using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblioteca_Popular_JuanXXIII;
using Biblioteca_Popular_JuanXXIII.Data;

namespace Biblioteca_Popular_JuanXXIII.Pages.Socies;

public class AltaModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public AltaModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    [BindProperty]
    public Socio Socio { get; set; } = default!;

    // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Socios.Add(Socio);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Inicio");
    }
}
