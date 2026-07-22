using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblioteca_Popular_JuanXXIII;
using Biblioteca_Popular_JuanXXIII.Data;

namespace Biblioteca_Popular_JuanXXIII.Pages.Socies;

public class DetallesModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public DetallesModel(ApplicationDbContext context)
    {
        _context = context;
    }

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
        else
        {
            Socio = socio;
        }

        return Page();
    }
}
