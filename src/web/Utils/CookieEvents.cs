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
            var firstname = context.Principal.Firstname();
            var lastname = context.Principal.Lastname();

            var id = await _mediator.Send(new SignInViaGoogle.Command(email));
            if (id == null)
            {
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
            else
            {
                (context.Principal.Identity as ClaimsIdentity).AddClaim(
                    new Claim(IdentityExtensions.ID_CLAIM_NAME, id.ToString())
                );
            }
        }
    }
}