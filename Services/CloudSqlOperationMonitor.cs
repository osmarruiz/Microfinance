using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microfinance.Services;
using Google.Apis.SQLAdmin.v1beta4.Data;

namespace Microfinance.Services;

public class CloudSqlOperationMonitor : BackgroundService
{
    private readonly ILogger<CloudSqlOperationMonitor> _logger;
    private readonly IServiceProvider _serviceProvider; // Para resolver servicios con ámbito
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30); // Sondear cada 30 segundos

    public CloudSqlOperationMonitor(ILogger<CloudSqlOperationMonitor> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("El monitor de operaciones de Cloud SQL está iniciando.");

        stoppingToken.Register(() =>
            _logger.LogInformation("El monitor de operaciones de Cloud SQL se está deteniendo."));

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_pollingInterval, stoppingToken);

            using (var scope = _serviceProvider.CreateScope())
            {
                var appStatusService = scope.ServiceProvider.GetRequiredService<ApplicationStatusService>();
                var cloudSqlService = scope.ServiceProvider.GetRequiredService<CloudSqlService>();

                if (appStatusService.IsUnderMaintenance && !string.IsNullOrEmpty(appStatusService.OperationId))
                {
                    try
                    {
                        _logger.LogInformation(
                            $"Verificando el estado de la operación de Cloud SQL: {appStatusService.OperationId}");
                        Operation operation = await cloudSqlService.GetOperationStatus(appStatusService.OperationId);

                        if (operation.Status == "DONE")
                        {
                            if (operation.Error == null)
                            {
                                _logger.LogInformation(
                                    $"La operación de Cloud SQL '{operation.Name}' completó exitosamente. Restaurando el acceso a la aplicación.");
                                appStatusService.SetMaintenanceMode(false); // Desactivar el modo mantenimiento
                            }
                            else
                            {
                                _logger.LogError(
                                    $"La operación de Cloud SQL '{operation.Name}' completó con errores: {operation.Error}. Por favor, revisa los registros de Cloud SQL.");
                                // Podrías querer mantenerlo en modo mantenimiento o establecer un mensaje de error específico
                                appStatusService.SetMaintenanceMode(false,
                                    message:
                                    "La restauración de la base de datos finalizó con errores. Contacte a soporte.");
                            }
                        }
                        else
                        {
                            _logger.LogInformation(
                                $"Estado de la operación de Cloud SQL '{operation.Name}': {operation.Status}");
                            // Si la operación sigue en curso, podrías actualizar el mensaje o simplemente esperar
                        }
                    }
                    catch (Google.GoogleApiException gex)
                    {
                        _logger.LogError(gex, "Error al verificar el estado de la operación de Cloud SQL: {Message}",
                            gex.Message);
                        // Si hay un error de API, decide si quieres salir del mantenimiento o reintentar.
                        // Para un error persistente, podrías querer mantener el mantenimiento, pero alertar.
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ocurrió un error inesperado al monitorear la operación de Cloud SQL.");
                        // Decide cómo manejar los errores inesperados durante el monitoreo
                    }
                }
            }
        }

        _logger.LogInformation("El monitor de operaciones de Cloud SQL se ha detenido.");
    }
}