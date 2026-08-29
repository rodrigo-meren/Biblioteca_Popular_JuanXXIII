using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages.Socies;

public class EditarModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    public EditarModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager; 
    }

    [BindProperty]
    public Socio Socio { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var socio = await _context.Socios.FirstOrDefaultAsync(m => m.Id == id);
        if (socio is null)
        {
            return NotFound();
        }
        Socio = socio;
        return Page();
    }

   
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Attach(Socio).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SocioExists(Socio.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        var registroAuditoria = new Models.Auditoria
        {
            UsuarioResponsable = User.Identity.Name, // Guarda el DNI del admin logueado
            Modulo = "Socios",
            Accion = "Edicion de Socio",
            Detalles = $"Se editó el socio: {Socio.Nombre} {Socio.Apellido} (DNI: {Socio.DniCarnet})."
        };
        _context.Auditorias.Add(registroAuditoria);

        return RedirectToPage("./Inicio");
    }

    // ==========================================
    // HANDLER PARA REGENERAR / REPARAR LA CUENTA
    // ==========================================
    public async Task<IActionResult> OnPostRegenerarPasswordAsync(int id)
    {
        var socio = await _context.Socios.FirstOrDefaultAsync(s => s.Id == id);
        if (socio == null)
        {
            TempData["MensajeError"] = "No se encontró la ficha del socio.";
            return RedirectToPage("./Inicio");
        }

        var stringDni = socio.DniCarnet.Trim();

        // 1. Buscamos al usuario en Identity por DNI por si existe pero está desvinculado
        var user = await _userManager.FindByNameAsync(stringDni);

        if (user == null)
        {
            // 2. SI NO EXISTE: Lo creamos desde cero como si fuera un alta nueva
            string correoIdentity = string.IsNullOrEmpty(socio.Correo) ? $"{stringDni}@biblioteca.local" : socio.Correo;

            user = new IdentityUser
            {
                UserName = stringDni,
                Email = correoIdentity,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, stringDni);

            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Socio");

                // Lo vinculamos a la ficha del socio y guardamos
                socio.IdentityUserId = user.Id;
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = $"¡Cuenta digital creada y vinculada desde cero! La clave para ingresar es: {stringDni}";
                return RedirectToPage(new { id = id });
            }
            else
            {
                string errores = string.Join(", ", createResult.Errors.Select(e => e.Description));
                TempData["MensajeError"] = $"Error al intentar crear la cuenta en Identity: {errores}";
                return RedirectToPage(new { id = id });
            }
        }
        else
        {
            // 3. SI YA EXISTE EN IDENTITY: Reparamos el vínculo si estaba roto
            if (string.IsNullOrEmpty(socio.IdentityUserId) || socio.IdentityUserId != user.Id)
            {
                socio.IdentityUserId = user.Id;
                await _context.SaveChangesAsync();
            }

            // 4. Reseteamos la contraseña al DNI
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, stringDni);

            if (resetResult.Succeeded)
            {
                TempData["MensajeExito"] = $"¡Vínculo reparado y contraseña restablecida! La nueva clave es: {stringDni}";
            }
            else
            {
                string errores = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                TempData["MensajeError"] = $"Error al resetear la contraseña: {errores}";
            }
        }

        return RedirectToPage(new { id = id });
    }
    private bool SocioExists(int id)
    {
        return _context.Socios.Any(e => e.Id == id);
    }
}