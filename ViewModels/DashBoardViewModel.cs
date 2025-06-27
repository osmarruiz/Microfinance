namespace Microfinance.ViewModels;

public class DashboardViewModel
{
    public int TotalActiveLoans { get; set; }
    public int TotalCustomers { get; set; }
    public decimal TotalPaymentsThisWeek { get; set; }
    public decimal TotalCollectedThisMonth { get; set; }
    public List<ChartData> LoansByStatus { get; set; }
    public List<MonthlyData> PaymentsLastSixMonths { get; set; }
    public List<ChartData> LoanDistribution { get; set; }
}

public class ChartData
{
    public string Label { get; set; }
    public int Value { get; set; }
}

public class MonthlyData
{
    public string Month { get; set; }
    public decimal Amount { get; set; }
}