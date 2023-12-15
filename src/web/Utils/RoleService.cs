using core.Account;
using core.fs.Adapters.Authentication;

namespace web.Utils;

public class RoleService : IRoleService
{
    private readonly string _adminEmail;

    public RoleService(string adminEmail)
    {
        _adminEmail = adminEmail;
    }
    
    public bool IsAdmin(UserState user) => user.Email == _adminEmail;

    public string GetAdminEmail() => _adminEmail;
}