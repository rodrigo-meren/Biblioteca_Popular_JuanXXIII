using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblioteca_Popular_JuanXXIII.Models;
using Biblioteca_Popular_JuanXXIII.Data;

namespace Biblioteca_Popular_JuanXXIII.Pages.LibroPages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Libro> Libro { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Libro = await _context.Libros.ToListAsync();
    }
}
