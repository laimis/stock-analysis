using System.Security.Claims;
using System.Threading.Tasks;
using core.fs.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace web.Utils
{
    public class CookieEvents : CookieAuthenticationEvents
    {
        private readonly ILogger<CookieEvents> _logger;
        private readonly Handler _service;

        public CookieEvents(
            ILogger<CookieEvents> logger,
            Handler service)
        {
            _logger = logger;
            _service = service;
        }

        public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> ctx)
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }

            return base.RedirectToLogin(ctx);
        }
        
        public override async Task SigningIn(CookieSigningInContext context)
        {
            var query = new LookupByEmail(context.Principal.Email());

            var response = await _service.Handle(query);

            if (response.IsOk == false)
            {
                _logger.LogCritical($"Unable to look up user {query.Email} for sign in");
                throw new System.Exception("Failed to sign in via google");
            }
            
            if (context.Principal is not { Identity: ClaimsIdentity identity })
            {
                _logger.LogCritical("Claims principal is not a claims identity, it's a {principal}", context.Principal?.GetType().Name);
                throw new System.Exception("Failed to sign in via google");
            }

            identity.AddClaim(
                new Claim(IdentityExtensions.ID_CLAIM_NAME, response.ResultValue.Id.ToString())
            );
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var query = new LookupByEmail(context.Principal.Email());

            var id = await _service.Handle(query);
            if (id.IsOk == false)
            {
                _logger.LogCritical($"Failed to validate principal");
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}
