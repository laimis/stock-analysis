using core.Account;
using core.fs.Adapters.Authentication;

namespace di;

public class RoleService(string? adminEmail) : IRoleService
{
    public bool IsAdmin(UserState user) => !string.IsNullOrWhiteSpace(adminEmail) && user.Email == adminEmail;

    public string? GetAdminEmail() => adminEmail;
}
