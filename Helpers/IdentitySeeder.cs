using Microsoft.AspNetCore.Identity;

namespace Microfinance.Helpers;

public static class IdentitySeeder
{
    public static async Task CrearRoles(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = { "Admin", "Salesperson", "Consultant" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}