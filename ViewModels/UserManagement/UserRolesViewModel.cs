namespace Microfinance.ViewModels.UserManagement;

public class UserRolesViewModel
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsLockedOut { get; set; }
    public IEnumerable<string> Roles { get; set; }
}