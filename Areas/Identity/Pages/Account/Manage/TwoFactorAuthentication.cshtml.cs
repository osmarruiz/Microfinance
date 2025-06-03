// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Microfinance.Areas.Identity.Pages.Account.Manage
{
    public class TwoFactorAuthenticationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<TwoFactorAuthenticationModel> _logger;

        public TwoFactorAuthenticationModel(
            UserManager<IdentityUser> userManager, 
            SignInManager<IdentityUser> signInManager, 
            ILogger<TwoFactorAuthenticationModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        ///     Esta API soporta la infraestructura de UI por defecto de ASP.NET Core Identity
        ///     y no está diseñada para ser usada directamente desde tu código.
        ///     Esta API puede cambiar o eliminarse en futuras versiones.
        /// </summary>
        public bool HasAuthenticator { get; set; }

        /// <summary>
        ///     Esta API soporta la infraestructura de UI por defecto de ASP.NET Core Identity
        ///     y no está diseñada para ser usada directamente desde tu código.
        ///     Esta API puede cambiar o eliminarse en futuras versiones.
        /// </summary>
        public int RecoveryCodesLeft { get; set; }

        /// <summary>
        ///     Esta API soporta la infraestructura de UI por defecto de ASP.NET Core Identity
        ///     y no está diseñada para ser usada directamente desde tu código.
        ///     Esta API puede cambiar o eliminarse en futuras versiones.
        /// </summary>
        [BindProperty]
        public bool Is2faEnabled { get; set; }

        /// <summary>
        ///     Esta API soporta la infraestructura de UI por defecto de ASP.NET Core Identity
        ///     y no está diseñada para ser usada directamente desde tu código.
        ///     Esta API puede cambiar o eliminarse en futuras versiones.
        /// </summary>
        public bool IsMachineRemembered { get; set; }

        /// <summary>
        ///     Esta API soporta la infraestructura de UI por defecto de ASP.NET Core Identity
        ///     y no está diseñada para ser usada directamente desde tu código.
        ///     Esta API puede cambiar o eliminarse en futuras versiones.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null;
            Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            IsMachineRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user);
            RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            await _signInManager.ForgetTwoFactorClientAsync();
            StatusMessage = "El navegador actual ha sido olvidado. Cuando inicies sesión nuevamente desde este navegador, se te pedirá tu código de autenticación de dos factores.";
            return RedirectToPage();
        }
    }
}