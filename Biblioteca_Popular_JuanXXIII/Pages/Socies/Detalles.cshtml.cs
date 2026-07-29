using Biblioteca_Popular_JuanXXIII;
using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages.Socies;

public class DetallesModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public DetallesModel(ApplicationDbContext context)
    {
        _context = context;
    }

    // Asegurate de tener estas propiedades
    public Socio Socio { get; set; } = default!;
    public IList<Cuota> Cuotas { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        Socio = await _context.Socios.FirstOrDefaultAsync(m => m.Id == id);

        if (Socio == null) return NotFound();

        // Traemos las cuotas de ESTE socie ordenadas
        Cuotas = await _context.Cuotas
            .Where(c => c.SocioId == id)
            .OrderByDescending(c => c.FechaVencimiento)
            .ToListAsync();

        return Page();
    }

    // Método para cobrar una cuota desde el detalle
    public async Task<IActionResult> OnPostRegistrarPagoAsync(int cuotaId, int socioId)
    {
        var cuota = await _context.Cuotas.FindAsync(cuotaId);
        if (cuota != null)
        {
            cuota.Estado = "Pagada";
            cuota.FechaPago = DateTime.Now;
            await _context.SaveChangesAsync();
        }
        // Recargamos la misma página del socie
        return RedirectToPage(new { id = socioId });
    }
}
