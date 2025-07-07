namespace Microfinance.Reports.DTO;

public class LoanSummaryDto
{
    public string Status { get; set; }
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
}