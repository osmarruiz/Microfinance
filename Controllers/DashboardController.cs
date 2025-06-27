using Microfinance.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microfinance.Data;
using Microfinance.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Microfinance.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ILogger<DashboardController> _logger;
    private readonly ApplicationDbContext _context;

    public DashboardController(ILogger<DashboardController> logger, ApplicationDbContext context)
    {
        _context = context;
        _logger = logger;
    }

public async Task<IActionResult> Index()
{
    // Verificar el rol del usuario
    var isAdmin = User.IsInRole("Admin");
    var isSalesperson = User.IsInRole("Salesperson");

    // Si es Salesperson, redirigir a la vista simplificada
    if (isSalesperson && !isAdmin)
    {
        return View("SalesDashboard");
    }

    // Usar DateTimeOffset.UtcNow para todas las fechas
    var now = DateTimeOffset.UtcNow;
    
    // Crear fechas de inicio en UTC (offset cero)
    var startOfMonth = new DateTimeOffset(new DateTime(now.Year, now.Month, 1), TimeSpan.Zero);
    var startOfWeek = new DateTimeOffset(now.AddDays(-(int)now.DayOfWeek).Date, TimeSpan.Zero);

    // Obtener datos para las tarjetas
    var dashboardData = new DashboardViewModel
    {
        TotalActiveLoans = await _context.Loans
            .Where(l => !l.IsDeleted && l.LoanStatus == "Activo")
            .CountAsync(),

        TotalCustomers = await _context.Customers
            .Where(c => !c.IsDeleted && c.IsActive)
            .CountAsync(),

        // Conversión explícita a UTC en las comparaciones
        TotalPaymentsThisWeek = await _context.Payments
            .Where(p => !p.IsDeleted && p.PaymentDate >= startOfWeek)
            .SumAsync(p => p.PaidAmount),

        TotalCollectedThisMonth = await _context.Payments
            .Where(p => !p.IsDeleted && p.PaymentDate >= startOfMonth)
            .SumAsync(p => p.PaidAmount),

        // Resto del código permanece igual
        LoansByStatus = await _context.Loans
            .Where(l => !l.IsDeleted)
            .GroupBy(l => l.LoanStatus)
            .Select(g => new ChartData
            {
                Label = g.Key,
                Value = g.Count()
            }).ToListAsync(),

        PaymentsLastSixMonths = await GetPaymentsLastMonths(6),

        LoanDistribution = await _context.Loans
            .Where(l => !l.IsDeleted)
            .GroupBy(l => l.PaymentFrequency)
            .Select(g => new ChartData
            {
                Label = g.Key,
                Value = g.Count()
            }).ToListAsync()
    };

    return View(dashboardData);
}

private async Task<List<MonthlyData>> GetPaymentsLastMonths(int months)
{
    var result = new List<MonthlyData>();
    var now = DateTimeOffset.UtcNow;
    
    for (int i = months - 1; i >= 0; i--)
    {
        var date = now.AddMonths(-i);
        var startDate = new DateTimeOffset(new DateTime(date.Year, date.Month, 1), TimeSpan.Zero);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        
        var total = await _context.Payments
            .Where(p => !p.IsDeleted && 
                       p.PaymentDate >= startDate && 
                       p.PaymentDate <= endDate)
            .SumAsync(p => (decimal?)p.PaidAmount) ?? 0;
        
        result.Add(new MonthlyData
        {
            Month = startDate.ToString("MMM yyyy"),
            Amount = total
        });
    }
    
    return result;
}

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}