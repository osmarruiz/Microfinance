using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microfinance.Services;

namespace Microfinance.Middleware
{
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly PathString _maintenancePath = new PathString("/Maintenance"); // Ruta a tu vista de mantenimiento

        public MaintenanceModeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationStatusService appStatusService)
        {
            // Excluye la propia página de mantenimiento y los archivos estáticos de la redirección
            if (appStatusService.IsUnderMaintenance &&
                !context.Request.Path.Equals(_maintenancePath) &&
                !context.Request.Path.StartsWithSegments("/lib") && // Excluir archivos estáticos (ej. CSS, JS)
                !context.Request.Path.StartsWithSegments("/css") &&
                !context.Request.Path.StartsWithSegments("/js") &&
                !context.Request.Path.StartsWithSegments("/images"))
            {
                context.Response.Redirect(_maintenancePath);
                return;
            }

            await _next(context);
        }
    }

    public static class MaintenanceModeMiddlewareExtensions
    {
        public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MaintenanceModeMiddleware>();
        }
    }
}