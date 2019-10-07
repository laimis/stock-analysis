using System.Security.Claims;

namespace web.Utils
{
    public static class IdentityExtensions
    {
        public static string Identifier(this System.Security.Claims.ClaimsPrincipal p)
        {
            return p.FindFirst(ClaimTypes.Email).Value;
        }
    }
}