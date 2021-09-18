using System.Security.Claims;
using System.Threading.Tasks;
using core.Account;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

namespace web.Utils
{
    public class CookieEvents : CookieAuthenticationEvents
    {
        private ILogger<CookieEvents> _logger;
        private IMediator _mediator;

        public CookieEvents(
            ILogger<CookieEvents> logger,
            IMediator mediator )
        {
            _logger = logger;
            _mediator = mediator;
        }
        
        public override async Task SigningIn(CookieSigningInContext context)
        {
            var email = context.Principal.Email();

            _logger.LogInformation($"Obtained email {email}");
            
            var id = await _mediator.Send(new SignInViaGoogle.Command(email));

            (context.Principal.Identity as ClaimsIdentity).AddClaim(
                new Claim(IdentityExtensions.ID_CLAIM_NAME, id.ToString())
            );
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var email = context.Principal.Email();

            _logger.LogInformation($"Obtained email {email}");
            
            var id = await _mediator.Send(new SignInViaGoogle.Command(email));
            if (id == null)
            {
                _logger.LogInformation($"Failed to validate principal {email}");

                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}