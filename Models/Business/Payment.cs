using System;
using System.Collections.Generic;

namespace Microfinance.Models.Business;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int LoanId { get; set; }

    public int InstallmentId { get; set; }

    public DateTime PaymentDate { get; set; }

    public decimal? PaidAmount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Reference { get; set; }

    public string CollectorId { get; set; } = null!;

    public bool? IsDeleted { get; set; }

    public virtual Installment Installment { get; set; } = null!;

    public virtual Loan Loan { get; set; } = null!;
}
