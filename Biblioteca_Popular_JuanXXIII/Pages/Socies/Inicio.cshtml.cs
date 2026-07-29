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

    [BindProperty(SupportsGet = true)]
    public int PaginaActual { get; set; } = 1;
    public int TotalPaginas { get; set; }
    public int RegistrosPorPagina { get; set; } = 20;

    public async Task OnGetAsync()
    {
        IQueryable<Socio> consulta = _context.Socios;

        // 2. Si el usuario escribió algo en el buscador, filtramos
        if (!string.IsNullOrEmpty(Buscar))
        {
            consulta = consulta.Where(s => s.Nombre.Contains(Buscar) ||
                                           s.Apellido.Contains(Buscar) ||
                                           s.DniCarnet.ToString().Contains(Buscar));
        }

        // 3. Calculamos la paginación basados en lo que quedó filtrado
        var cantidadTotal = await consulta.CountAsync();
        TotalPaginas = (int)Math.Ceiling(cantidadTotal / (double)RegistrosPorPagina);

        // Asegurarnos de que no estemos en una página que no existe
        if (PaginaActual < 1) PaginaActual = 1;
        if (PaginaActual > TotalPaginas && TotalPaginas > 0) PaginaActual = TotalPaginas;

        // 4. Recortamos los 20 registros que corresponden a la página actual y ejecutamos
        Socio = await consulta
            .OrderBy(s => s.Apellido) // Los ordenamos alfabéticamente
            .Skip((PaginaActual - 1) * RegistrosPorPagina)
            .Take(RegistrosPorPagina)
            .ToListAsync();
    }
        // --- ESTE ES EL MÉTODO NUEVO QUE RECIBE EL CSV ---
        public async Task<IActionResult> OnPostImportarAsync(IFormFile archivoCsv)
        {
            if (archivoCsv == null || archivoCsv.Length == 0)
            {
                return RedirectToPage();
            }

        // Cambiamos InvariantCulture por "es-AR" para que entienda el formato de fecha dd/MM/yyyy
        var config = new CsvConfiguration(new CultureInfo("es-AR"))
        {
            Delimiter = ";", // <--- ¡ESTE ES EL CAMBIO CLAVE!
            HasHeaderRecord = true
        };
        using (var stream = new StreamReader(archivoCsv.OpenReadStream()))
            using (var csv = new CsvReader(stream, config))
            {
                // Le decimos a CsvHelper que use nuestras reglas de nombres
                csv.Context.RegisterClassMap<SocioMap>();

                // Ahora sí, lee y convierte todo de una
                var records = csv.GetRecords<Socio>().ToList();

                foreach (var socio in records)
                {
                    // Verificamos si el DNI ya existe para no duplicar ni que explote la base
                    var existeSocio = await _context.Socios.AnyAsync(s => s.DniCarnet == socio.DniCarnet);

                    if (!existeSocio)
                    {
                        // 1. Guardamos el socio en tu tabla
                        _context.Socios.Add(socio);
                        await _context.SaveChangesAsync();

                        // 2. Le creamos la cuenta en el sistema de seguridad
                        var stringDni = socio.DniCarnet.ToString();
                        var nuevoUsuario = new IdentityUser { UserName = stringDni };

                        // Crea el usuario con el DNI como contraseña
                        var resultado = await _userManager.CreateAsync(nuevoUsuario, stringDni);

                        if (resultado.Succeeded)
                        {
                            // Lo asignamos al rol de Socio
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
        // 1. Buscamos al socie en la base de datos (asumiendo que la clave primaria se llama Id)
        var socio = await _context.Socios.FindAsync(id);

        if (socio != null)
        {
            // 2. (Recomendado) Buscamos su usuario de acceso por el DNI y lo eliminamos
            // para que no quede la cuenta de alguien que ya no es socie.
            var usuarioSocio = await _userManager.FindByNameAsync(socio.DniCarnet.ToString());
            if (usuarioSocio != null)
            {
                await _userManager.DeleteAsync(usuarioSocio);
            }

            // 3. Lo borramos de la tabla física
            _context.Socios.Remove(socio);
            await _context.SaveChangesAsync();
        }

        // 4. ¡La instrucción clave para recargar la lista de forma segura!
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

        // Le decimos explícitamente: si viene vacío (""), tomalo como Nulo
        Map(m => m.FechaNacimiento).Name("Fecha_Nacimiento")
            .TypeConverterOption.NullValues("")
            .Optional();

        Map(m => m.Correo).Name("Correo").Optional();
        Map(m => m.Telefono).Name("Telefono").Optional();
        Map(m => m.Direccion).Name("Direccion").Optional();

        // Hacemos lo mismo con las otras fechas por si alguna viene vacía
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

