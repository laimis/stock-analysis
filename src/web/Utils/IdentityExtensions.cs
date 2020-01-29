using System.Security.Claims;

namespace web.Utils
{
    public static class IdentityExtensions
    {
        public const string ID_CLAIM_NAME = "userid";
        
        public static string Identifier(this System.Security.Claims.ClaimsPrincipal p)
        {
            if (p == null)
            {
                return null;
            }

            var claim = p.FindFirst(ID_CLAIM_NAME);
            if (claim == null)
            {
                return null;
            }
            
            return claim.Value;
        }
    }
}