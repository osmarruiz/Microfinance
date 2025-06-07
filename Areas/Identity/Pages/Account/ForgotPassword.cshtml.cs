// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Microfinance.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await SendPasswordResetEmail(Input.Email, callbackUrl);
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
        
        private async Task SendPasswordResetEmail(string email, string callbackUrl)
{
    var emailBody = $@"
    <!DOCTYPE html>
    <html lang='es'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>Restablecer contraseña - Microfinance</title>
        <style>
            /* Mismos estilos que en el correo de bienvenida */
            body {{
                font-family: 'Arial', sans-serif;
                line-height: 1.6;
                color: #333;
                max-width: 600px;
                margin: 0 auto;
                padding: 20px;
            }}
            .header {{
                background-color: #4CAF50;
                color: white;
                padding: 20px;
                text-align: center;
                border-radius: 5px 5px 0 0;
                margin-bottom: 0;
            }}
            .content {{
                padding: 25px;
                background-color: #f9f9f9;
                border-radius: 0 0 5px 5px;
                border: 1px solid #ddd;
                border-top: none;
            }}
            .action-button {{
                display: inline-block;
                padding: 12px 24px;
                background-color: #4CAF50;
                color: white !important;
                text-decoration: none;
                border-radius: 4px;
                font-weight: bold;
                margin: 15px 0;
            }}
            .footer {{
                margin-top: 25px;
                font-size: 12px;
                color: #777;
                text-align: center;
                border-top: 1px solid #eee;
                padding-top: 15px;
            }}
            .callout {{
                background-color: #f0f8ff;
                border-left: 4px solid #3498db;
                padding: 12px;
                margin: 15px 0;
            }}
        </style>
    </head>
    <body>
        <div class='header'>
            <h2 style='margin:0;'>Restablecer contraseña</h2>
        </div>
        <div class='content'>
            <p>Hola,</p>
            <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta.</p>
            
            <div style='text-align: center; margin: 25px 0;'>
                <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' 
                   class='action-button'>
                   Restablecer contraseña
                </a>
            </div>
            
            <div class='callout'>
                <p>Si no solicitaste este cambio, puedes ignorar este mensaje. El enlace expirará en 24 horas por motivos de seguridad.</p>
            </div>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} Microfinance. Todos los derechos reservados.</p>
            <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
        </div>
    </body>
    </html>";

    await _emailSender.SendEmailAsync(
        email,
        "Restablecer contraseña - Microfinance",
        emailBody);
}
    }
}