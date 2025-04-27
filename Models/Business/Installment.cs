using System;
using System.Collections.Generic;

namespace Microfinance.Models.Business;

public partial class Installment
{
    public int InstallmentId { get; set; }

    public int LoanId { get; set; }

    public int InstallmentNumber { get; set; }

    public decimal InstallmentAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal? LateFee { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime PaymentDate { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Loan Loan { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
