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

    [Column("payment_frequency")]
    [MaxLength(20)]
    public string PaymentFrequency { get; set; } = null!;

    [Column("loan_status")]
    [MaxLength(20)]
    public string LoanStatus { get; set; } = LoanStatusEnum.Activo;

    [Column("is_deleted")] public bool IsDeleted { get; set; }

    // Relaciones
    public Customer Customer { get; set; } = null!;
    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
    public ICollection<CollectionManagement> CollectionManagements { get; set; } = new List<CollectionManagement>();

    public IdentityUser Seller { get; set; } = null!;
}

public static class LoanStatusEnum
{
    public const string Activo = "Activo";
    public const string Vencido = "Vencido";
    public const string Pagado = "Pagado";
    public const string Cancelado = "Cancelado";
}

public static class PaymentFrequencyEnum
{
    public const string Diario = "Diario";
    public const string Semanal = "Semanal";
    public const string Quincenal = "Quincenal";
    public const string Mensual = "Mensual";
}