using Microsoft.AspNetCore.Identity;

namespace Microfinance.Reports.DTO;

public class CollectorPerformanceData
{
    public IdentityUser Collector { get; set; }
    public int Collections { get; set; }
    public int SuccessfulCollections { get; set; }
    public decimal AmountCollected { get; set; }
}