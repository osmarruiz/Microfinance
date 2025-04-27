using System;
using System.Collections.Generic;

namespace Microfinance.Models.Business;

public partial class Loan
{
    public int LoanId { get; set; }

    public int CustomerId { get; set; }

    public string SellerId { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal CurrentBalance { get; set; }

    public decimal InterestRate { get; set; }

    public int TermMonths { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime DueDate { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<CollectionManagement> CollectionManagements { get; set; } = new List<CollectionManagement>();

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<Installment> Installments { get; set; } = new List<Installment>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
