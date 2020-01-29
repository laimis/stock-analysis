using System;
using System.Security.Claims;
using System.Threading.Tasks;
using core.Account;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

namespace web.Utils
{
    public class CookieEvents : CookieAuthenticationEvents
    {
        private ILogger<CookieEvents> _logger;
        private IAccountStorage _accounts;

        public CookieEvents(
            ILogger<CookieEvents> logger,
            IAccountStorage accounts)
        {
            _logger = logger;
            _accounts = accounts;
        }
        
        public override async Task SigningIn(CookieSigningInContext context)
        {
            var i = context.Principal.Identity as ClaimsIdentity;

            var claim = i.FindFirst(ClaimTypes.Email);
            if (claim == null)
            {
                _logger.LogCritical("Failed to find email claim for " + i.Name);
            }

            var email = claim.Value;

            var u = await _accounts.GetUser(claim.Value);
            if (u == null)
            {
                u = new User(email);

                await _accounts.Save(u);
            }

            i.AddClaim(new Claim(IdentityExtensions.ID_CLAIM_NAME, u.State.Id.ToString()));
        }
    }
}