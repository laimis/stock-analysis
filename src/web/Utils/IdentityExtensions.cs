using System;
using System.Security.Claims;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;

namespace web.Utils
{
    public static class IdentityExtensions
    {
        public const string ID_CLAIM_NAME = "userid";

        public static UserId Identifier(this ClaimsPrincipal p)
        {
            var guid = GetClaimValue(p, ID_CLAIM_NAME);
            if (guid == null)
            {
                throw new Exception($"User is not authenticated. Missing claim: {ID_CLAIM_NAME}");
            }

            return UserId.NewUserId(new Guid(guid));
        }

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