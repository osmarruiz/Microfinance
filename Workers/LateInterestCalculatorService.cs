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
                    
                    // Obtener cuotas vencidas no pagadas con sus préstamos
                    // Solo cuotas que vencen ANTES de hoy (ayer o antes)
                    var overdueInstallments = await dbContext.Installments
                        .Include(i => i.Loan)
                        .Where(i => i.DueDate.Date < DateTime.UtcNow.Date && 
                                   i.InstallmentStatus == InstallmentStatusEnum.Vencida &&
                                   i.PaymentDate == null)
                        .ToListAsync(stoppingToken);

                    if (overdueInstallments.Any())
                    {
                        int updatedCount = 0;
                        
                        foreach (var installment in overdueInstallments)
                        {
                            try
                            {
                                var newLateInterest = CalculateLateInterest(installment);
                                
                                // Solo actualizar si el valor cambió significativamente
                                if (Math.Abs(installment.LateInterestAmount - newLateInterest) > 0.01m)
                                {
                                    installment.LateInterestAmount = newLateInterest;
                                    updatedCount++;
                                    
                                    _logger.LogInformation($"Cuota {installment.InstallmentId}: Interés moratorio actualizado a {newLateInterest:C}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error calculando interés moratorio para cuota {installment.InstallmentId}");
                            }
                        }

                        if (updatedCount > 0)
                        {
                            await dbContext.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"Actualizados intereses moratorios para {updatedCount} cuotas.");
                        }
                        else
                        {
                            _logger.LogInformation("No se requirieron actualizaciones de intereses moratorios.");
                        }
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

    private decimal CalculateLateInterest(Installment installment)
    {
        // 1. Calcular tasa efectiva anual
        var monthlyRate = installment.Loan.MonthlyInterestRate / 100m;
        var effectiveAnnualRate = (decimal)Math.Pow((double)(1 + monthlyRate), 12) - 1;

        // 2. Calcular tasa moratoria anual (Art. 73 - 25% adicional)
        var lateAnnualRate = effectiveAnnualRate * 1.25m;

        // 3. Convertir a tasa diaria efectiva
        var dailyLateRate = (decimal)Math.Pow((double)(1 + lateAnnualRate), 1.0 / 365) - 1;

        // 4. Calcular días de atraso (mínimo 1 día)
        var daysLate = Math.Max((DateTime.UtcNow.Date - installment.DueDate.Date).Days, 1);

        // 5. Calcular capital pendiente (cuota sin pagar)
        var principalDue = Math.Max(
            installment.PrincipalAmount + installment.NormalInterestAmount, 
            0
        );

        // 6. Calcular interés moratorio
        var lateInterest = principalDue * dailyLateRate * daysLate;

        // 7. Aplicar redondeo
        var integerPart = Math.Truncate(lateInterest);
        var decimalPart = lateInterest - integerPart;

        if (decimalPart <= 0.39m)
        {
            return integerPart;
        }
        else
        {
            return integerPart + 1.00m;
        }
    }
}