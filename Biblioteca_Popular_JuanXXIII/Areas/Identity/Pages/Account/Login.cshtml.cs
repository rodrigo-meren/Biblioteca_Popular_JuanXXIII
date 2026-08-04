// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Biblioteca_Popular_JuanXXIII.Data;

namespace Biblioteca_Popular_JuanXXIII.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class InputModel
    {
        [Required(ErrorMessage = "El DNI es obligatorio.")]
        [Display(Name = "DNI")]
        public string Dni { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }
    }
    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (ModelState.IsValid)
        {
            // 1. Limpiamos espacios invisibles accidentales por si acaso
            string dniLimpio = Input.Dni?.Trim() ?? "";
            string passLimpio = Input.Password?.Trim() ?? "";

            var result = await _signInManager.PasswordSignInAsync(dniLimpio, passLimpio, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                if (returnUrl != null && returnUrl.Contains("Logout"))
                {
                    returnUrl = "~/";
                }
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToPage("./Lockout");
            }
            else if (result.IsNotAllowed)
            {
                // Esto pasa si Identity requiere que el correo esté confirmado por link
                ModelState.AddModelError(string.Empty, "DIAGNÓSTICO: Identity bloqueó el acceso (Cuenta no permitida/Falta confirmar email).");
                return Page();
            }
            else
            {
                // DIAGNÓSTICO PROFUNDO: Le preguntamos a la base de datos qué pasó realmente
                var user = await _signInManager.UserManager.FindByNameAsync(dniLimpio);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, $"DIAGNÓSTICO: El DNI '{dniLimpio}' NO existe en la base de datos de seguridad.");
                }
                else
                {
                    // El usuario existe, verifiquemos si la contraseña es el problema
                    var passCheck = await _signInManager.UserManager.CheckPasswordAsync(user, passLimpio);
                    if (!passCheck)
                    {
                        ModelState.AddModelError(string.Empty, $"DIAGNÓSTICO: DNI encontrado, pero la CONTRASEÑA es incorrecta.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, $"DIAGNÓSTICO: Usuario y clave correctos, pero falló por otra restricción interna.");
                    }
                }
                return Page();
            }
        }

        // If we got this far, something failed, redisplay form
        return Page();
    }
}

