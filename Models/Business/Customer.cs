using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microfinance.Models.Business;

[Table("customers", Schema = "business")]
public class Customer
{
    [Key] [Column("customer_id")] public int CustomerId { get; set; }

    [Column("full_name")] [MaxLength(100)] public string FullName { get; set; } = null!;

    [Column("id_card")] [MaxLength(15)] public string IdCard { get; set; } = null!;

    [Column("phone_number")]
    [MaxLength(15)]
    public string PhoneNumber { get; set; } = null!;

    [Column("address")] [MaxLength(200)] public string Address { get; set; } = null!;

    [Column("email")] [MaxLength(100)] public string? Email { get; set; }

    [Column("is_active")] public bool IsActive { get; set; } = true;

    [Column("is_deleted")] public bool IsDeleted { get; set; }

    // Relaciones
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}