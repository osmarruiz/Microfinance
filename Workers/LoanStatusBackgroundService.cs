using Microfinance.Data;
using Microfinance.Models.Business;
using Microsoft.EntityFrameworkCore;

public class LoanStatusBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LoanStatusBackgroundService> _logger;

    public LoanStatusBackgroundService(
        IServiceProvider services,
        ILogger<LoanStatusBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Verificando préstamos vencidos...");

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Obtener préstamos vencidos no marcados como "Vencido"
                var overdueLoans = await dbContext.Loans
                    .Where(l => l.DueDate < DateTime.UtcNow && l.LoanStatus == LoanStatusEnum.Activo )
                    .ToListAsync(stoppingToken);

                if (overdueLoans.Any())
                {
                    foreach (var loan in overdueLoans)
                    {
                        loan.LoanStatus = LoanStatusEnum.Vencido;
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Actualizados {overdueLoans.Count} préstamos a 'Vencido'.");
                }
                else
                {
                    _logger.LogInformation("No hay préstamos vencidos por actualizar.");
                }
            }

            // Esperar 24 horas antes de la próxima verificación
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}

