using Biblioteca_Popular_JuanXXIII.Models;
using Biblioteca_Popular_JuanXXIII.Data;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Biblioteca_Popular_JuanXXIII.Pages.Socies;

public class AltaModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public AltaModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public Socio Socio { get; set; } = default!;

    public IActionResult OnGet()
    {
        return Page();
    }

    // ==========================================
    // MÉTODO 1: IMPORTACIÓN MASIVA DESDE CSV
    // ==========================================
    public async Task<IActionResult> OnPostImportarAsync(IFormFile archivoCsv)
    {
        if (archivoCsv == null || archivoCsv.Length == 0)
        {
            TempData["MensajeError"] = "Por favor, seleccioná un archivo CSV válido.";
            return RedirectToPage();
        }

        var config = new CsvConfiguration(new CultureInfo("es-AR"))
        {
            Delimiter = ";",
            HasHeaderRecord = true
        };

        try
        {
            using (var stream = new StreamReader(archivoCsv.OpenReadStream()))
            using (var csv = new CsvReader(stream, config))
            {
                csv.Context.RegisterClassMap<SocioMap>();
                var records = csv.GetRecords<Socio>().ToList();

                foreach (var socio in records)
                {
                    // 1. FORZAMOS EL SOCIO COMO ACTIVO
                    socio.Activo = true;

                    var existeSocio = await _context.Socios.AnyAsync(s => s.DniCarnet == socio.DniCarnet);

                    if (!existeSocio)
                    {
                        var stringDni = socio.DniCarnet.ToString();

                        // Generamos un correo alternativo si el CSV viene con el campo vacío
                        string correoIdentity = string.IsNullOrEmpty(socio.Correo) ? $"{stringDni}@biblioteca.local" : socio.Correo;

                        // 2. CREAMOS LA CUENTA DE IDENTITY PRIMERO
                        var nuevoUsuario = new IdentityUser
                        {
                            UserName = stringDni,
                            Email = correoIdentity,
                            EmailConfirmed = true
                        };

                        var resultado = await _userManager.CreateAsync(nuevoUsuario, stringDni);

                        if (resultado.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(nuevoUsuario, "Socio");

                            // 3. VINCULAMOS EL ID AL SOCIO FÍSICO
                            socio.IdentityUserId = nuevoUsuario.Id;
                        }

                        // 4. GUARDAMOS EN LA TABLA SOCIOS
                        _context.Socios.Add(socio);

                        try
                        {
                            await _context.SaveChangesAsync();
                        }
                        catch (DbUpdateException dbEx)
                        {
                            // Si la base de datos rechaza a este socio específico, lo sacamos del contexto 
                            // para que no trabe a los siguientes socios de la lista
                            _context.Entry(socio).State = EntityState.Detached;

                            // Y guardamos el error para mostrarlo en la pantalla
                            TempData["MensajeError"] = $"Se guardaron algunos, pero hubo un error al guardar el DNI {socio.DniCarnet}. Revisá que los campos obligatorios (como Nombre o Apellido) no estén vacíos en el CSV.";

                            // Cortamos la ejecución para que puedas revisar
                            return RedirectToPage();
                        }
                    }
                }
                TempData["MensajeExito"] = "¡Importación completada! Los socios están activos y vinculados.";
            }
        }
        catch (Exception ex)
        {
            // Si el archivo está mal formado, falla la lectura, etc.
            TempData["MensajeError"] = $"Error al procesar el archivo: {ex.Message}";
            return Page(); // <-- CLAVE: Usamos Page() para que muestre el error sin recargar
        }

        // Si todo salió bien, ahí sí redirigimos limpiamente
        return RedirectToPage();
    }

    // ==========================================
    // MÉTODO 2: ALTA MANUAL DE UN SOLO SOCIO
    // ==========================================
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // 1. FORZAMOS EL ESTADO ACTIVO
        Socio.Activo = true;

        var stringDni = Socio.DniCarnet.ToString();
        string correoIdentity = string.IsNullOrEmpty(Socio.Correo) ? $"{stringDni}@biblioteca.local" : Socio.Correo;

        // 2. CREAMOS LA CUENTA DIGITAL PRIMERO
        var nuevoUsuario = new IdentityUser
        {
            UserName = stringDni,
            Email = correoIdentity,
            EmailConfirmed = true
        };

        var resultadoIdentity = await _userManager.CreateAsync(nuevoUsuario, stringDni);

        if (resultadoIdentity.Succeeded)
        {
            await _userManager.AddToRoleAsync(nuevoUsuario, "Socio");

            // 3. VINCULAMOS EL ID DE LA CUENTA A LA FICHA DEL SOCIO
            Socio.IdentityUserId = nuevoUsuario.Id;
        }

        // 4. GUARDAMOS EL SOCIO EN LA BD
        _context.Socios.Add(Socio);
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = "Socio registrado y cuenta creada con éxito.";
        return RedirectToPage("./Inicio");
    }
}