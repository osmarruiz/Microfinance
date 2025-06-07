namespace Microfinance.ViewModels.UserManagement;

public class ManageRolesViewModel
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public List<RoleViewModel> Roles { get; set; }
}