using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Microfinance.Models.Business;

[Table("collection_management", Schema = "business")]
public class CollectionManagement
{
    [Key]
    [Column("collection_id")]
    [DisplayName("ID de Gestión")]
    public int CollectionId { get; set; }

    [Column("loan_id")]
    [DisplayName("Préstamo")]
    public int LoanId { get; set; }

    [Column("collector_id")]
    [MaxLength(200)]
    [DisplayName("Cobrador")]
    public string CollectorId { get; set; } = null!;

    [Column("management_date")]
    [DisplayName("Fecha de Gestión")]
    public DateTimeOffset ManagementDate { get; set; }

    [Column("management_result")]
    [MaxLength(200)]
    [DisplayName("Resultado")]
    public string? ManagementResult { get; set; }

    [Column("notes")]
    [MaxLength(200)]
    [DisplayName("Notas")]
    public string? Notes { get; set; }

    [Column("is_deleted")]
    [DisplayName("Eliminado")]
    public bool IsDeleted { get; set; }

    // Relaciones
    [DisplayName("Préstamo")]
    public Loan? Loan { get; set; } = null!;

    [DisplayName("Cobrador")]
    public IdentityUser? Collector { get; set; } = null!;
}