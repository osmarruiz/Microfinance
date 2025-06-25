// Workers/InstallmentStatusBackgroundService.cs

using Microfinance.Data;
using Microfinance.Models.Business;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class InstallmentStatusBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InstallmentStatusBackgroundService> _logger;

    public InstallmentStatusBackgroundService(
        IServiceProvider services,
        ILogger<InstallmentStatusBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Verificando cuotas vencidas...");

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Obtener cuotas vencidas no marcadas como "Vencida"
                var overdueInstallments = await dbContext.Installments
                    .Where(i => i.DueDate < DateTime.UtcNow && 
                               i.InstallmentStatus == InstallmentStatusEnum.Pendiente)
                    .ToListAsync(stoppingToken);

                if (overdueInstallments.Any())
                {
                    foreach (var installment in overdueInstallments)
                    {
                        installment.InstallmentStatus = InstallmentStatusEnum.Vencida;
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Actualizadas {overdueInstallments.Count} cuotas a estado 'Vencida'.");
                }
                else
                {
                    _logger.LogInformation("No hay cuotas vencidas por actualizar.");
                }
            }

            // Esperar 24 horas antes de la próxima verificación
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}