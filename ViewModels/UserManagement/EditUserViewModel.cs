using System.ComponentModel.DataAnnotations;

namespace Microfinance.ViewModels.UserManagement;

public class EditUserViewModel
{
    public string Id { get; set; }

    [Required]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; }

    
    [Required]
    [RegularExpression(@"^\+505[0-9]{8}$", ErrorMessage = "Formato inválido. Debe comenzar con +505 seguido de 8 dígitos")]
    [StringLength(12, ErrorMessage = "El teléfono no puede exceder 12 caracteres")]
    [Display(Name = "Numero de Teléfono")]
    public string PhoneNumber { get; set; }

    public bool IsLockedOut { get; set; }
}