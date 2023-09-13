using System.Security.Claims;
using System.Threading.Tasks;
using core.fs.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

namespace web.Utils
{
    public class CookieEvents : CookieAuthenticationEvents
    {
        private readonly ILogger<CookieEvents> _logger;
        private readonly Status.Handler _service;

        public CookieEvents(
            ILogger<CookieEvents> logger,
            core.fs.Account.Status.Handler service)
        {
            _logger = logger;
            _service = service;
        }
        
        public override async Task SigningIn(CookieSigningInContext context)
        {
            var email = context.Principal.Email();

            var response = await _service.Handle(email);

            if (response.IsOk == false)
            {
                _logger.LogCritical($"Unable to look up user {email} for sign in");
                throw new System.Exception("Failed to sign in via google");
            }
            
            if (context.Principal is not { Identity: ClaimsIdentity identity })
            {
                _logger.LogCritical("Claims principal is not a claims identity, it's a {principal}", context.Principal == null ? null : context.Principal.GetType().Name);
                throw new System.Exception("Failed to sign in via google");
            }

            identity.AddClaim(
                new Claim(IdentityExtensions.ID_CLAIM_NAME, response.Success.Id.ToString())
            );
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var email = context.Principal.Email();

            var id = await _service.Handle(email);
            if (id.IsOk == false)
            {
                _logger.LogCritical($"Failed to validate principal");
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}