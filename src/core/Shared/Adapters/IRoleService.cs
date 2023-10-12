using core.Account;

namespace core.Shared.Adapters;

public interface IRoleService
{
    bool IsAdmin(UserState user);
    string GetAdminEmail();
}