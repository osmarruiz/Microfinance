using System.ComponentModel.DataAnnotations;

namespace Microfinance.ViewModels.UserManagement;

public class CreateUserViewModel
{
    [Required]
    [Display(Name = "Correo Electrónico")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    public string Email { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "TemporalPassword123!";
    
    [Required]
    [Display(Name = "Numero de telefono")]
    [RegularExpression(@"^\+505[0-9]{8}$", ErrorMessage = "Formato inválido. Debe comenzar con +505 seguido de 8 dígitos")]
    [StringLength(12, ErrorMessage = "El teléfono no puede exceder 12 caracteres")]
    public string PhoneNumber { get; set; }
}