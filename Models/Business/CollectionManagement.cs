using System;
using System.Collections.Generic;

namespace Microfinance.Models.Business;

public partial class CollectionManagement
{
    public int CollectionId { get; set; }

    public int LoanId { get; set; }

    public string CollectorId { get; set; } = null!;

    public DateOnly? ManagementDate { get; set; }

    public string? ManagementResult { get; set; }

    public string? Notes { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Loan Loan { get; set; } = null!;
}
