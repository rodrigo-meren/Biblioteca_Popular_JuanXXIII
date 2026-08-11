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

    public Socio Socio { get; set; } = default!;
    public IList<Cuota> Cuotas { get; set; } = default!;

    // 1. Agregamos la lista de préstamos al modelo
    public IList<Prestamo> Prestamos { get; set; } = default!;

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

        // 2. Traemos los préstamos de ESTE socie, incluyendo el Libro para ver el Título
        Prestamos = await _context.Prestamos
            .Include(p => p.Libro)
            .Where(p => p.SocioId == id)
            .OrderByDescending(p => p.FechaPrestamo)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostRegistrarPagoAsync(int cuotaId, int socioId)
    {
        var cuota = await _context.Cuotas.FindAsync(cuotaId);
        if (cuota != null)
        {
            cuota.Estado = "Pagada";
            cuota.FechaPago = DateTime.Now;
            await _context.SaveChangesAsync();
        }
        return RedirectToPage(new { id = socioId });
    }
}