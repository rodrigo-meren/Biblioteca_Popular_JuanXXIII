using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages.Auditoria
{
    // Solo los administradores pueden ver quién hizo qué
    [Authorize(Roles = "Administrador")]
    public class InicioModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public InicioModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Models.Auditoria> Registros { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Traemos los últimos 100 movimientos, del más nuevo al más viejo
            Registros = await _context.Auditorias
                .OrderByDescending(a => a.FechaHora)
                .Take(100)
                .ToListAsync();
        }
    }
}