using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microfinance.Models.Business;

[Table("customers", Schema = "business")]
public class Customer
{
    [Key]
    [Column("customer_id")]
    [Display(Name = "ID Cliente")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres")]
    [Column("full_name")]
    [Display(Name = "Nombre Completo")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "La cédula es obligatoria")]
    [Column("id_card")]
    [Display(Name = "Cédula/DNI")]
    [RegularExpression(@"^[0-9]{13}[A-Za-z]$", ErrorMessage = "Formato inválido. Debe contener 13 números seguidos de 1 letra")]
    [StringLength(14, ErrorMessage = "La cédula no puede exceder 14 caracteres")]
    public string IdCard { get; set; } = null!;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [Column("phone_number")]
    [Display(Name = "Teléfono")]
    [RegularExpression(@"^\+505[0-9]{8}$", ErrorMessage = "Formato inválido. Debe comenzar con +505 seguido de 8 dígitos")]
    [StringLength(12, ErrorMessage = "El teléfono no puede exceder 12 caracteres")]
    public string PhoneNumber { get; set; } = null!;

    [Required(ErrorMessage = "La dirección es obligatoria")]
    [Column("address")]
    [Display(Name = "Dirección")]
    [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
    public string Address { get; set; } = null!;

    [Column("email")]
    [Display(Name = "Correo Electrónico")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
    public string? Email { get; set; }

    [Column("is_active")]
    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;

    [Column("is_deleted")]
    [Display(Name = "Eliminado")]
    public bool IsDeleted { get; set; }

    // Relaciones
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}