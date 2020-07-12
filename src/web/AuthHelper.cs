using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using web.Utils;

namespace web
{
    public class AuthHelper
    {
        internal static void Configure(IConfiguration configuration, IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "Google";
            })
            .AddCookie(opt =>
            {
                opt.Cookie.Name = "tw";
                opt.Cookie.Domain = "nightingaletrading.com";
                opt.EventsType = typeof(CookieEvents);
            })
            .AddGoogle("Google", options =>
            {
                options.ClientId = configuration.GetValue<string>("GoogleClientId");
                options.ClientSecret = configuration.GetValue<string>("GoogleSecret");
            });

            services.AddAuthorization(opt => 
                opt.AddPolicy("admin", p => p.RequireClaim(ClaimTypes.Email, "laimis@gmail.com"))
            );
        }
    }
}