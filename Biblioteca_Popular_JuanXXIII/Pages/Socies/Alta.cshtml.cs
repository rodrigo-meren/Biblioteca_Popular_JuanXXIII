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
            return Page();
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
                    socio.Activo = true;

                    // Verificamos si la ficha física ya existe
                    var existeSocio = await _context.Socios.AnyAsync(s => s.DniCarnet == socio.DniCarnet);

                    if (!existeSocio)
                    {
                        var stringDni = socio.DniCarnet.ToString();
                        string correoIdentity = string.IsNullOrEmpty(socio.Correo) ? $"{stringDni}@biblioteca.local" : socio.Correo;

                        // BUSCAMOS SI LA CUENTA FANTASMA YA EXISTE
                        var usuarioExistente = await _userManager.FindByNameAsync(stringDni);

                        if (usuarioExistente == null)
                        {
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
                                socio.IdentityUserId = nuevoUsuario.Id;
                            }
                        }
                        else
                        {
                            // Si el fantasma existe, lo adoptamos
                            socio.IdentityUserId = usuarioExistente.Id;
                        }

                        // Guardamos la ficha en la BD
                        _context.Socios.Add(socio);

                        try
                        {
                            await _context.SaveChangesAsync();
                        }
                        catch (DbUpdateException)
                        {
                            _context.Entry(socio).State = EntityState.Detached;
                            TempData["MensajeError"] = $"Se frenó en el DNI {socio.DniCarnet}. Revisá que no falten datos obligatorios.";
                            return Page();
                        }
                    }
                }
                TempData["MensajeExito"] = "¡Importación masiva completada con éxito!";
            }
        }
        catch (Exception ex)
        {
            TempData["MensajeError"] = $"Error al procesar el archivo CSV: {ex.Message}";
            return Page();
        }

        return RedirectToPage("./Inicio");
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

        Socio.Activo = true;
        var stringDni = Socio.DniCarnet.ToString();
        string correoIdentity = string.IsNullOrEmpty(Socio.Correo) ? $"{stringDni}@biblioteca.local" : Socio.Correo;

        // BUSCAMOS SI LA CUENTA FANTASMA YA EXISTE
        var usuarioExistente = await _userManager.FindByNameAsync(stringDni);

        if (usuarioExistente == null)
        {
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
                Socio.IdentityUserId = nuevoUsuario.Id;
            }
            else
            {
                var errores = string.Join(" | ", resultadoIdentity.Errors.Select(e => e.Description));
                TempData["MensajeError"] = $"No se pudo crear la cuenta digital del socio. Motivo: {errores}";
                return Page();
            }
        }
        else
        {
            // Si la cuenta ya existía de un intento fallido anterior, la reciclamos
            Socio.IdentityUserId = usuarioExistente.Id;
        }

        // GUARDAMOS EL SOCIO EN LA BD
        _context.Socios.Add(Socio);

        try
        {
            await _context.SaveChangesAsync();
            TempData["MensajeExito"] = "¡Socio registrado y vinculado con éxito!";
            return RedirectToPage("./Inicio");
        }
        catch (DbUpdateException dbEx)
        {
            var errorReal = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
            TempData["MensajeError"] = $"La base de datos rechazó al socio. Motivo: {errorReal}";
            return Page();
        }
    }
}