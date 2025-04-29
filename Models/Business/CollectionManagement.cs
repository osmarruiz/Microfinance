using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Microfinance.Models.Business;

[Table("collection_management", Schema = "business")]
public class CollectionManagement
{
    [Key] [Column("collection_id")] public int CollectionId { get; set; }

    [Column("loan_id")] public int LoanId { get; set; }

    [Column("collector_id")]
    [MaxLength(200)]
    public string CollectorId { get; set; } = null!;

    [Column("management_date")] public DateTimeOffset ManagementDate { get; set; }

    [Column("management_result")]
    [MaxLength(200)]
    public string? ManagementResult { get; set; }

    [Column("notes")] [MaxLength(200)] public string? Notes { get; set; }

    [Column("is_deleted")] public bool IsDeleted { get; set; }

    // Relaciones
    public Loan Loan { get; set; } = null!;

    public IdentityUser Collector { get; set; } = null!;
}