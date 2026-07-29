using Biblioteca_Popular_JuanXXIII;
using Biblioteca_Popular_JuanXXIII.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII.Pages.Socies;

public class AltaModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager; // AGREGAMOS ESTO

    // Modificamos el constructor para recibir ambas cosas
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

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // 1. Guardamos el socio físico en tu tabla normal
        _context.Socios.Add(Socio);
        await _context.SaveChangesAsync();

        // 2. Creamos su cuenta digital automáticamente
        // Usamos el DNI convertido a texto para el usuario
        var stringDni = Socio.DniCarnet.ToString();
        var nuevoUsuario = new IdentityUser { UserName = stringDni };

        // Creamos la cuenta pasando el usuario y la contraseña (que también es el DNI)
        var resultadoIdentity = await _userManager.CreateAsync(nuevoUsuario, stringDni);

        if (resultadoIdentity.Succeeded)
        {
            // 3. Si se creó bien, le damos el rol de "Socio" para que los candados funcionen
            await _userManager.AddToRoleAsync(nuevoUsuario, "Socio");
        }

        return RedirectToPage("./Inicio");
    }
}
