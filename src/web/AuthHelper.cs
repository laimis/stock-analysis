using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using web.Utils;

namespace web
{
    public class AuthHelper
    {
        internal static void Configure(IConfiguration configuration, IServiceCollection services, string adminEmail)
        {
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                throw new System.Exception("ADMINEmail is not set");
            }
            
            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "Google";
            })
            .AddCookie(opt =>
            {
                opt.Cookie.Name = "tw";
                opt.EventsType = typeof(CookieEvents);
            });

            var googleClientId = configuration.GetValue<string>("GoogleClientId");
            if (googleClientId != null)
            {
                authBuilder.AddGoogle("Google", options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = configuration.GetValue<string>("GoogleSecret");
                    options.ReturnUrlParameter = "returnUrl";
                });   
            }

            services.AddAuthorization(opt => 
                opt.AddPolicy("admin", p => p.RequireClaim(ClaimTypes.Email, adminEmail))
            );
        }
    }
}