using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Microfinance.Models.Business;

[Table("installments", Schema = "business")]
public class Installment
{
    [Key] [Column("installment_id")] public int InstallmentId { get; set; }

    [Column("loan_id")] public int LoanId { get; set; }

    [Column("installment_number")] public int InstallmentNumber { get; set; }

    [Column("installment_amount")]
    [Precision(10, 2)]
    public decimal InstallmentAmount { get; set; }

    [Column("paid_amount")]
    [Precision(10, 2)]
    public decimal PaidAmount { get; set; }

    [Column("late_fee")]
    [Precision(10, 2)]
    public decimal LateFee { get; set; }

    [Column("due_date")] public DateTimeOffset DueDate { get; set; }

    [Column("payment_date")] public DateTimeOffset? PaymentDate { get; set; }

    [Column("installment_status")]
    [MaxLength(20)]
    public string InstallmentStatus { get; set; } = InstallmentStatusEnum.Pendiente;

    [Column("is_deleted")] public bool IsDeleted { get; set; }

    // Relaciones
    public Loan Loan { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}


public static class InstallmentStatusEnum
{
    public const string Pendiente = "Pendiente";
    public const string Pagada = "Pagada";
    public const string Vencida = "Vencida";
}