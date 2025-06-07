using Microfinance.ViewModels.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")] // Solo accesible por administradores
public class UsersController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailSender _emailSender;

    public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IEmailSender emailSender)
    {
        _emailSender = emailSender;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: Users
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var users = await _userManager.Users
            .Where(u => u.Id != currentUser.Id) // Excluir al usuario actual
            .ToListAsync();

        var userRolesViewModel = new List<UserRolesViewModel>();
        
        foreach (var user in users)
        {
            var thisViewModel = new UserRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsLockedOut = user.LockoutEnd > DateTime.Now,
                Roles = await _userManager.GetRolesAsync(user)
            };
            userRolesViewModel.Add(thisViewModel);
        }
        
        return View(userRolesViewModel);
    }

    // GET: Users/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true // Si quieres que no requiera confirmación
            };
        
            var result = await _userManager.CreateAsync(user, model.Password);
        
            if (result.Succeeded)
            {
                
                await _userManager.AddToRoleAsync(user, "Salesperson");
                // Enviar correo con las credenciales
                await SendWelcomeEmail(user, model.Password);
            
                return RedirectToAction(nameof(Index));
            }
        
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    
        return View(model);
    }
    
   private async Task SendWelcomeEmail(IdentityUser user, string password)
{
    var emailBody = $@"
    <!DOCTYPE html>
    <html lang='es'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>Bienvenido a Microfinance</title>
        <style>
            /* Estilos base consistentes */
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
            .credentials {{
                background-color: #fff;
                border: 1px solid #e0e0e0;
                padding: 15px;
                margin: 20px 0;
                border-radius: 4px;
                border-left: 4px solid #4CAF50;
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
            <h2 style='margin:0;'>¡Bienvenido a Microfinance!</h2>
        </div>
        <div class='content'>
            <p>Hola,</p>
            <p>Tu cuenta ha sido creada exitosamente. Aquí están tus credenciales de acceso:</p>
            
            <div class='credentials'>
                <p><strong>Usuario:</strong> {user.Email}</p>
                <p><strong>Contraseña temporal:</strong> {password}</p>
            </div>
            
            <div class='callout'>
                <p>Por seguridad, te recomendamos cambiar tu contraseña después de iniciar sesión por primera vez.</p>
            </div>
            
            <p>Si no reconoces esta actividad, por favor contacta a nuestro equipo de soporte inmediatamente.</p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} Microfinance. Todos los derechos reservados.</p>
            <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
        </div>
    </body>
    </html>";

    await _emailSender.SendEmailAsync(
        user.Email,
        "Bienvenido a Microfinance - Tus credenciales de acceso",
        emailBody);
}

    // GET: Users/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsLockedOut = user.LockoutEnd > DateTime.Now
        };

        return View(model);
    }

    // POST: Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Email = model.Email;
            user.UserName = model.Email; // Normalmente el username es el email
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View(model);
    }

    // GET: Users/ManageRoles/5
    public async Task<IActionResult> ManageRoles(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        try
        {
            // 1. Primero materializamos TODOS los roles
            var allRoles = await _roleManager.Roles
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            // 2. Obtenemos TODOS los roles del usuario de una sola vez
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRolesSet = new HashSet<string>(userRoles, StringComparer.OrdinalIgnoreCase);

            // 3. Construimos el modelo
            var model = new ManageRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Roles = allRoles.Select(r => new RoleViewModel
                {
                    RoleId = r.Id,
                    RoleName = r.Name,
                    IsSelected = userRolesSet.Contains(r.Name)
                }).ToList()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error interno al procesar los roles");
        }
    }

    // POST: Users/ManageRoles/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageRoles(string id, ManageRolesViewModel model)
    {
        if (id != model.UserId)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var result = await _userManager.RemoveFromRolesAsync(user, userRoles);

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "No se pudieron eliminar los roles actuales del usuario");
            return View(model);
        }

        result = await _userManager.AddToRolesAsync(user, 
            model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName));

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "No se pudieron agregar los roles seleccionados al usuario");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Users/ResetPassword/5
    public async Task<IActionResult> ResetPassword(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var model = new ResetPasswordViewModel
        {
            UserId = user.Id,
            Email = user.Email
        };

        return View(model);
    }

    // POST: Users/ResetPassword/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            return NotFound();
        }

        // Generar token de reseteo
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        // Resetear la contraseña
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    // POST: Users/Lock/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Users/Unlock/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, null);

        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Users/Delete/5
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // POST: Users/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(user);
    }
}