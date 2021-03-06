using System;
using System.Security.Claims;

namespace web.Utils
{
    public static class IdentityExtensions
    {
        public const string ID_CLAIM_NAME = "userid";
        
        public static Guid Identifier(this System.Security.Claims.ClaimsPrincipal p)
            => new Guid(GetClaimValue(p, ID_CLAIM_NAME) ?? Guid.Empty.ToString());

        public static string Email(this System.Security.Claims.ClaimsPrincipal p) 
            => GetClaimValue(p, ClaimTypes.Email);

        public static string Firstname(this System.Security.Claims.ClaimsPrincipal p) 
            => GetClaimValue(p, ClaimTypes.GivenName);

        public static string Lastname(this System.Security.Claims.ClaimsPrincipal p) 
            => GetClaimValue(p, ClaimTypes.Surname);

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