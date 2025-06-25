using Microfinance.Data;
using Microfinance.Models.Business;
using Microsoft.EntityFrameworkCore;

namespace Microfinance.Workers;

public class LateInterestCalculatorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LateInterestCalculatorService> _logger;

    public LateInterestCalculatorService(
        IServiceProvider services,
        ILogger<LateInterestCalculatorService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Recalculando intereses moratorios...");
            
            try
            {
                using (var scope = _services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    // Obtener cuotas vencidas no pagadas
                    var overdueInstallments = await dbContext.Installments
                        .Where(i => i.DueDate < DateTime.UtcNow && 
                                   i.InstallmentStatus == InstallmentStatusEnum.Vencida &&
                                   i.PaymentDate == null)
                        .ToListAsync(stoppingToken);

                    if (overdueInstallments.Any())
                    {
                        foreach (var installment in overdueInstallments)
                        {
                            installment.LateInterestAmount = 0.01m;
                            _logger.LogInformation("Recalculando intereses moratorios..." + installment.InstallmentId + " - " + installment.LateInterestAmount);
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Recalculados intereses para {overdueInstallments.Count} cuotas.");
                    }
                    else
                    {
                        _logger.LogInformation("No hay cuotas pendientes de recálculo.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recalcular intereses moratorios");
            }

            // Esperar 24 horas antes de la próxima ejecución
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}