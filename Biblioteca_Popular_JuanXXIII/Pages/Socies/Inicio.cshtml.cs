using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblioteca_Popular_JuanXXIII;
using Biblioteca_Popular_JuanXXIII.Data;

namespace Biblioteca_Popular_JuanXXIII.Pages.Socies;

public class InicioModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public InicioModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Socio> Socio { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Socio = await _context.Socios.ToListAsync();
    }
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var socio = await _context.Socios.FindAsync(id);

        if (socio != null)
        {
            _context.Socios.Remove(socio);
            await _context.SaveChangesAsync();
        }

        // Recarga la grilla
        return RedirectToPage("./Inicio");
    }
}
