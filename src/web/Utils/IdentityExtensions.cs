using System.Security.Claims;

namespace web.Utils
{
    public static class IdentityExtensions
    {
        public static string Identifier(this System.Security.Claims.ClaimsPrincipal p)
        {
            if (p == null)
            {
                return null;
            }

            var claim = p.FindFirst(ClaimTypes.Email);
            if (claim == null)
            {
                return null;
            }
            
            return claim.Value;
        }
    }
}