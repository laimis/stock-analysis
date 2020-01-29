using System.Security.Claims;

namespace web.Utils
{
    public static class IdentityExtensions
    {
        public const string ID_CLAIM_NAME = "userid";
        
        public static string Identifier(this System.Security.Claims.ClaimsPrincipal p)
        {
            return GetClaimValue(p, ID_CLAIM_NAME);
        }

        public static string Email(this System.Security.Claims.ClaimsPrincipal p)
        {
            return GetClaimValue(p, ClaimTypes.Email);
        }

        private static string GetClaimValue(ClaimsPrincipal p, string name)
        {
            if (p == null)
            {
                return null;
            }

            var claim = p.FindFirst(name);
            if (claim == null)
            {
                return null;
            }

            return claim.Value;
        }
    }
}