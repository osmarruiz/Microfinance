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
    [Key] 
    [Column("loan_id")] 
    public int LoanId { get; set; }

    [Required(ErrorMessage = "El cliente es requerido")]
    [Column("customer_id")]
    [Display(Name = "Cliente")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "El vendedor es requerido")]
    [MaxLength(200, ErrorMessage = "El ID del vendedor es demasiado largo")] 
    [Column("seller_id")] 
    public string SellerId { get; set; } = null!;

    [Required(ErrorMessage = "El monto es requerido")]
    [Range(4000.00, 30000.00, ErrorMessage = "El monto debe estar entre {1} y {2}")]
    [Column("principal_amount")] 
    [Display(Name = "Monto del Préstamo")]
    [Precision(10, 2)] 
    public decimal PrincipalAmount { get; set; }

    [Required(ErrorMessage = "El interes actual es requerido")]
    [Range(0, double.MaxValue, ErrorMessage = "El interes no puede ser negativo")]
    [Column("normal_interest_amount")]
    [Precision(10, 2)]
    public decimal NormalInterestAmount { get; set; }
    
    [Required(ErrorMessage = "El interes moratirio actual es requerido")]
    [Range(0, double.MaxValue, ErrorMessage = "El interes moratorio no puede ser negativo")]
    [Column("late_interest_amount")]
    [Precision(10, 2)]
    public decimal LateInterestAmount { get; set; } = 0;

    [Required(ErrorMessage = "La tasa de interés es requerida")]
    [Range(0, 100, ErrorMessage = "La tasa de interés debe estar entre 0 y 100")]
    [Column("monthly_interest_rate")]
    [Display(Name = "Interés Moratorio")]
    [Precision(10, 2)]
    public decimal MonthlyInterestRate { get; set; }

    [Required(ErrorMessage = "El plazo en meses es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El plazo debe ser de al menos 1 mes")]
    [Column("term_months")] 
    [Display(Name = "Plazo (Meses)")]
    public int TermMonths { get; set; }

    [Column("start_date")] 
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow;

    [Required(ErrorMessage = "La fecha de vencimiento es requerida")]
    [Column("due_date")] 
    public DateTimeOffset DueDate { get; set; }

    [Required(ErrorMessage = "La frecuencia de pago es requerida")]
    [MaxLength(20)]
    [Column("payment_frequency")]
    [Display(Name = "Frecuencia de Pago")]
    public string PaymentFrequency { get; set; } = null!;

    [MaxLength(20)]
    [Column("loan_status")]
    public string LoanStatus { get; set; } = LoanStatusEnum.Activo;

    [Column("is_deleted")] 
    public bool IsDeleted { get; set; } = false;

    // Relaciones
    public Customer? Customer { get; set; } = null!;
    public ICollection<Installment>? Installments { get; set; } = new List<Installment>();
    public ICollection<CollectionManagement>? CollectionManagements { get; set; } = new List<CollectionManagement>();

    public IdentityUser? Seller { get; set; } = null!;
}

public static class LoanStatusEnum
{
    public const string Activo = "Activo";
    public const string Vencido = "Vencido";
    public const string Cancelado = "Cancelado";
}

public static class PaymentFrequencyEnum
{
    public const string Diario = "Diario";
    public const string Semanal = "Semanal";
    public const string Quincenal = "Quincenal";
    public const string Mensual = "Mensual";
}