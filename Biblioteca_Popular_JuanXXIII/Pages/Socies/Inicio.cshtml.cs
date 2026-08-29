using Biblioteca_Popular_JuanXXIII;
using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Biblioteca_Popular_JuanXXIII.Pages.Socies;

[Authorize(Roles = "Administrador, Bibliotecario")]
public class InicioModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public InicioModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public IList<Socio> Socio { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string Buscar { get; set; }

    // --- NUEVO FILTRO DE ESTADO ---
    [BindProperty(SupportsGet = true)]
    public string FiltroEstado { get; set; } = "Activos";

    [BindProperty(SupportsGet = true)]
    public int PaginaActual { get; set; } = 1;
    public int TotalPaginas { get; set; }
    public int RegistrosPorPagina { get; set; } = 20;

    public async Task OnGetAsync()
    {
        // 1. Empezamos la consulta general
        IQueryable<Socio> consulta = _context.Socios;

        // 2. Aplicamos el filtro según lo que eligió el usuario en el menú desplegable
        if (FiltroEstado == "Inactivos")
        {
            consulta = consulta.Where(s => s.Activo == false);
        }
        else if (FiltroEstado == "Todos")
        {
            // No aplicamos filtro, mostramos todos
        }
        else
        {
            // Por defecto, mostramos solo los activos
            consulta = consulta.Where(s => s.Activo == true);
        }

        // 3. Si el usuario escribió algo en el buscador, filtramos
        if (!string.IsNullOrEmpty(Buscar))
        {
            var terminoBusqueda = Buscar.ToLower();
            consulta = consulta.Where(s => s.Nombre.ToLower().Contains(terminoBusqueda) ||
                                           s.Apellido.ToLower().Contains(terminoBusqueda) ||
                                           s.DniCarnet.Contains(terminoBusqueda));
        }

        // 4. Calculamos la paginación basados en lo que quedó filtrado
        var cantidadTotal = await consulta.CountAsync();
        TotalPaginas = (int)Math.Ceiling(cantidadTotal / (double)RegistrosPorPagina);

        if (PaginaActual < 1) PaginaActual = 1;
        if (PaginaActual > TotalPaginas && TotalPaginas > 0) PaginaActual = TotalPaginas;

        Socio = await consulta
            .OrderBy(s => s.Apellido)
            .Skip((PaginaActual - 1) * RegistrosPorPagina)
            .Take(RegistrosPorPagina)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostImportarAsync(IFormFile archivoCsv)
    {
        if (archivoCsv == null || archivoCsv.Length == 0)
        {
            return RedirectToPage();
        }

        var config = new CsvConfiguration(new CultureInfo("es-AR"))
        {
            Delimiter = ";",
            HasHeaderRecord = true
        };

        using (var stream = new StreamReader(archivoCsv.OpenReadStream()))
        using (var csv = new CsvReader(stream, config))
        {
            csv.Context.RegisterClassMap<SocioMap>();
            var records = csv.GetRecords<Socio>().ToList();

            foreach (var socio in records)
            {
                var existeSocio = await _context.Socios.AnyAsync(s => s.DniCarnet == socio.DniCarnet);

                if (!existeSocio)
                {
                    _context.Socios.Add(socio);
                    await _context.SaveChangesAsync();

                    var stringDni = socio.DniCarnet.ToString();
                    var nuevoUsuario = new IdentityUser { UserName = stringDni };
                    var resultado = await _userManager.CreateAsync(nuevoUsuario, stringDni);

                    if (resultado.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(nuevoUsuario, "Socio");
                    }
                }
            }
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGenerarLoteAsync()
    {
        string mesActual = DateTime.Now.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-AR"));
        decimal montoFijo = 1500.00m;

        var sociesActivos = await _context.Socios.ToListAsync();

        foreach (var socio in sociesActivos)
        {
            bool yaTieneCuota = await _context.Cuotas
                .AnyAsync(c => c.SocioId == socio.Id && c.MesCorrespondiente == mesActual);

            if (!yaTieneCuota)
            {
                var nuevaCuota = new Cuota
                {
                    SocioId = socio.Id,
                    Monto = montoFijo,
                    MesCorrespondiente = mesActual,
                    FechaVencimiento = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 10),
                    Estado = "Pendiente"
                };
                _context.Cuotas.Add(nuevaCuota);
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToPage("./Inicio");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var socio = await _context.Socios.FindAsync(id);

        if (socio != null)
        {
            var usuarioSocio = await _userManager.FindByNameAsync(socio.DniCarnet.ToString());
            if (usuarioSocio != null)
            {
                await _userManager.DeleteAsync(usuarioSocio);
            }

            _context.Socios.Remove(socio);
            await _context.SaveChangesAsync();
        }
        var registroAuditoria = new Models.Auditoria
        {
            UsuarioResponsable = User.Identity.Name, // Guarda el DNI del admin logueado
            Modulo = "Socios",
            Accion = "Baja de Socio",
            Detalles = $"Se dio de baja al socio: {socio.Nombre} {socio.Apellido} (DNI: {socio.DniCarnet})."
        };
        _context.Auditorias.Add(registroAuditoria);

        return RedirectToPage();
    }

    // --- NUEVO MÉTODO PARA REACTIVAR ---
    public async Task<IActionResult> OnPostReactivarAsync(int id)
    {
        var socio = await _context.Socios.FindAsync(id);
        if (socio != null)
        {
            // Lo volvemos a poner como activo
            socio.Activo = true;
            await _context.SaveChangesAsync();
        }
        var registroAuditoria = new Models.Auditoria
        {
            UsuarioResponsable = User.Identity.Name, // Guarda el DNI del admin logueado
            Modulo = "Socios",
            Accion = "Activacíon de Socio",
            Detalles = $"Se activo al socio: {socio.Nombre} {socio.Apellido} (DNI: {socio.DniCarnet})."
        };
        _context.Auditorias.Add(registroAuditoria);
        return RedirectToPage();
    }
}

public class SocioMap : ClassMap<Socio>
{
    public SocioMap()
    {
        Map(m => m.DniCarnet).Name("DNI_Carnet");
        Map(m => m.Apellido).Name("Apellido");
        Map(m => m.Nombre).Name("Nombre");

        Map(m => m.FechaNacimiento).Name("Fecha_Nacimiento")
            .TypeConverterOption.NullValues("")
            .Optional();

        Map(m => m.Correo).Name("Correo").Optional();
        Map(m => m.Telefono).Name("Telefono").Optional();
        Map(m => m.Direccion).Name("Direccion").Optional();

        Map(m => m.FechaAlta).Name("Fecha_Alta")
            .TypeConverterOption.NullValues("")
            .Optional();

        Map(m => m.FechaVencimiento).Name("Fecha_Vencimiento")
            .TypeConverterOption.NullValues("")
            .Optional();

        Map(m => m.Categoria).Name("Categoria").Optional();
        Map(m => m.Genero).Name("Genero").Optional();
    }
}