using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Microfinance.Models.Business;

[Table("loans", Schema = "business")]
public class Loan
{
    [Key] [Column("loan_id")] public int LoanId { get; set; }

    [Column("customer_id")] public int CustomerId { get; set; }

    [MaxLength(200)] [Column("seller_id")] public string SellerId { get; set; } = null!;

    [Column("amount")] [Precision(10, 2)] public decimal Amount { get; set; }

    [Column("current_balance")]
    [Precision(10, 2)]
    public decimal CurrentBalance { get; set; }

    [Column("interest_rate")]
    [Precision(10, 2)]
    public decimal InterestRate { get; set; }

    [Column("term_months")] public int TermMonths { get; set; }

    [Column("start_date")] public DateTimeOffset StartDate { get; set; } = DateTimeOffset.Now;

    [Column("due_date")] public DateTimeOffset DueDate { get; set; }

    [Column("payment_frequency")] public PaymentFrequencyEnum PaymentFrequency { get; set; }

    [Column("loan_status")] public LoanStatusEnum LoanStatus { get; set; } = LoanStatusEnum.Activo;

    [Column("is_deleted")] public bool IsDeleted { get; set; }

    // Relaciones
    public Customer Customer { get; set; } = null!;
    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
    public ICollection<CollectionManagement> CollectionManagements { get; set; } = new List<CollectionManagement>();

    public IdentityUser Seller { get; set; } = null!;
}

public enum PaymentFrequencyEnum
{
    Diario,
    Semanal,
    Quincenal,
    Mensual
}

public enum LoanStatusEnum
{
    Activo,
    Vencido,
    Pagado,
    Cancelado
}