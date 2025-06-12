using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Microfinance.Models.Business;

[Table("payments", Schema = "business")]
public class Payment
{
    [Key]
    [Column("payment_id")]
    [DisplayName("ID de Pago")]
    public int PaymentId { get; set; }

    [Column("installment_id")]
    [DisplayName("ID de Cuota")]
    public int InstallmentId { get; set; }

    [Column("payment_date")]
    [DisplayName("Fecha de Pago")]
    public DateTimeOffset PaymentDate { get; set; } = DateTimeOffset.Now;

    [Column("paid_amount")]
    [Precision(10, 2)]
    [DisplayName("Monto Pagado")]
    public decimal PaidAmount { get; set; }

    [Column("reference")]
    [MaxLength(100)]
    [DisplayName("Referencia")]
    public string? Reference { get; set; }

    [Column("collector_id")]
    [MaxLength(200)]
    [DisplayName("ID de Cobrador")]
    public string CollectorId { get; set; } = null!;

    [Column("is_deleted")]
    [DisplayName("Eliminado")]
    public bool IsDeleted { get; set; }

    // Relaciones
    [DisplayName("Cuota")]
    public Installment? Installment { get; set; } = null!;
    
    [DisplayName("Cobrador")]
    public IdentityUser? Collector { get; set; } = null!;
}