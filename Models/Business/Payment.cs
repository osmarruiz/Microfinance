using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Microfinance.Models.Business;

[Table("payments", Schema = "business")]
public class Payment
{
    [Key] [Column("payment_id")] public int PaymentId { get; set; }

    [Column("installment_id")] public int InstallmentId { get; set; }

    [Column("payment_date")] public DateTimeOffset PaymentDate { get; set; } = DateTimeOffset.Now;

    [Column("paid_amount")]
    [Precision(10, 2)]
    public decimal PaidAmount { get; set; }

    [Column("reference")] [MaxLength(100)] public string? Reference { get; set; }

    [Column("collector_id")] [MaxLength(200)] public string CollectorId { get; set; } = null!;

    [Column("is_deleted")] public bool IsDeleted { get; set; }

    // Relaciones
    public Installment Installment { get; set; } = null!;
    
    public IdentityUser Collector { get; set; } = null!;
}