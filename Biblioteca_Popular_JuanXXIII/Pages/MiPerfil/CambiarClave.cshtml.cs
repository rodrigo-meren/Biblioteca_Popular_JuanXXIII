using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Biblioteca_Popular_JuanXXIII.Pages.MiPerfil
{
    [Authorize(Roles = "Socio, Administrador")]
    public class CambiarClaveModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public CambiarClaveModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [TempData]
        public string Mensaje { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña Actual")]
            public string ClaveActual { get; set; } = string.Empty;

            [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nueva Contraseña")]
            public string NuevaClave { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar Nueva Contraseña")]
            [Compare("NuevaClave", ErrorMessage = "La nueva contraseña y la confirmación no coinciden.")]
            public string ConfirmarClave { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Logout", new { area = "Identity" });
            }

            // Identity hace todo el trabajo pesado acá adentro
            var result = await _userManager.ChangePasswordAsync(user, Input.ClaveActual, Input.NuevaClave);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, "La contraseña actual es incorrecta o la nueva no cumple los requisitos.");
                }
                return Page();
            }

            // Refrescamos la sesión para que no lo desloguee por cambiar la clave
            await _signInManager.RefreshSignInAsync(user);

            // Le mandamos un mensaje de éxito a la pantalla de inicio
            TempData["Mensaje"] = "¡Tu contraseña se actualizó correctamente!";

            return RedirectToPage("./Inicio");
        }
    }
}