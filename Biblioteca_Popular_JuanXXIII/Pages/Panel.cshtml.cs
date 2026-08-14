using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models; // Necesario para Prestamo
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages;

[Authorize]
public class PanelModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public PanelModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int TotalSocios { get; set; }
    public int TotalLibros { get; set; }
    public int PrestamosActivos { get; set; }
    public int CuotasPendientes { get; set; }
    public IList<Reserva> ReservasActivas { get; set; } = new List<Reserva>();
    public IList<Resena> UltimasResenas { get; set; } = new List<Resena>();
    public IList<Prestamo> UltimosPrestamos { get; set; } = default!;
    public IList<Recordatorio> NotasPendientes { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        // 2. EL AGENTE DE TRÁNSITO: Si el que entró es un Socio, lo mandamos a su perfil
        if (User.IsInRole("Socio") && !User.IsInRole("Administrador") && !User.IsInRole("Bibliotecario"))
        {
            return RedirectToPage("/MiPerfil/Inicio");
        }
        TotalSocios = await _context.Socios.CountAsync(s => s.Activo == true);
        TotalLibros = await _context.Libros.CountAsync();

        PrestamosActivos = await _context.Prestamos
             .Where(p => p.FechaDevolucion == null)
             .CountAsync();

        CuotasPendientes = await _context.Cuotas
        .Where(c => c.Estado == "Pendiente" || c.Estado == "Vencida")
        .CountAsync();

        // Traemos los últimos 5 préstamos registrados
        UltimosPrestamos = await _context.Prestamos
            .Include(p => p.Socio) // Trae los datos del socie
            .Include(p => p.Libro) // Trae los datos del libro
            .OrderByDescending(p => p.FechaPrestamo) // Los más nuevos primero
            .Take(5)
            .ToListAsync();

        // 2. Traemos solo las notas que NO están resueltas
        NotasPendientes = await _context.Recordatorios
            .Where(r => r.Resuelto == false)
            .OrderByDescending(r => r.EsUrgente) // Las urgentes van arriba
            .ThenBy(r => r.FechaCreacion)
            .ToListAsync();
        ReservasActivas = await _context.Reservas
    .Include(r => r.Libro)
    .Include(r => r.Socio)
    .Where(r => r.Estado == "Pendiente de retiro" || r.Estado == "En espera")
    .OrderBy(r => r.FechaSolicitud)
    .ToListAsync();

        UltimasResenas = await _context.Resenas
    .Include(r => r.Socio)
    .Include(r => r.Libro)
    .OrderByDescending(r => r.FechaPublicacion)
    .Take(10) // Solo traemos las 10 más recientes para no saturar el panel
    .ToListAsync();

        return Page();
    }

    // 3. Método para crear una nota rápida
    public async Task<IActionResult> OnPostCrearNotaAsync(string textoNota, bool esUrgente)
    {
        if (!string.IsNullOrWhiteSpace(textoNota))
        {
            var nuevaNota = new Recordatorio
            {
                Texto = textoNota,
                EsUrgente = esUrgente
            };
            _context.Recordatorios.Add(nuevaNota);
            await _context.SaveChangesAsync();
        }
        return RedirectToPage(); // Recargamos el panel
    }

    // 4. Método para marcar como resuelta (tachar)
    public async Task<IActionResult> OnPostResolverNotaAsync(int id)
    {
        var nota = await _context.Recordatorios.FindAsync(id);
        if (nota != null)
        {
            nota.Resuelto = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToPage();
    }
   

    public async Task<IActionResult> OnPostModerarResenaAsync(int id)
    {
        var resena = await _context.Resenas.FindAsync(id);

        if (resena != null)
        {
            // Esto funciona como un interruptor de luz. Si es true, lo pasa a false. Si es false, lo pasa a true.
            resena.Aprobada = !resena.Aprobada;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}