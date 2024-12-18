using System.Security.Claims;
using System.Threading.Tasks;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using web.Utils;

namespace web
{
    public class AuthHelper
    {
        internal static void Configure(IConfiguration configuration, IServiceCollection services)
        {
            var adminEmail = configuration.GetValue<string>("ADMINEmail");
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
                    var authEndpoint = options.Events.OnRedirectToAuthorizationEndpoint;
                    
                    options.Events.OnRedirectToAuthorizationEndpoint = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }

                        return authEndpoint(context);
                    };
                });   
            }

            services.AddAuthorization(opt => 
                opt.AddPolicy("admin", p => p.RequireClaim(ClaimTypes.Email, adminEmail))
            );
        }
    }
    
    // needed for hangfire
    public class MyAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            return httpContext.User.Identity?.IsAuthenticated ?? false;
        }
    }
}
