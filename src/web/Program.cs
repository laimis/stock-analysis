using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json.Serialization;
using di;
using Hangfire;
using Hangfire.PostgreSql;
using web.Utils;

namespace web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            var configuration = builder.Configuration;
            
            // Create a temporary logger factory for setup
            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole());
            var logger = loggerFactory.CreateLogger(typeof(Program).FullName);

            AuthHelper.Configure(configuration, builder.Services);
            
            Jobs.AddJobs(configuration, builder.Services, logger);

            builder.Services
                .AddControllers(
                    o => {
                        o.InputFormatters.Add(new TextPlainInputFormatter());
                })
                .AddJsonOptions(o => {
                    
                    // go through this assembly types, and find all types that are JsonConverter<T>
                    // and add them to the options
                    foreach(var type in typeof(Program).Assembly.GetTypes())
                    {
                        if (type.IsSubclassOf(typeof(JsonConverter)) && !type.IsAbstract)
                        {
                            var converter = Activator.CreateInstance(type);
                            o.JsonSerializerOptions.Converters.Add((JsonConverter)converter);
                        }
                    }
                });

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddHealthChecks()
                .AddCheck<HealthCheck>("storage based health check");

            DIHelper.RegisterServices(configuration, builder.Services, logger);
            builder.Services.AddSingleton<CookieEvents>();
            
            // configure hangfire
            var cnn = configuration.GetValue<string>("DB_CNN");
            if (!string.IsNullOrEmpty(cnn))
            {
                GlobalConfiguration.Configuration.UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(cnn));
            }

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseForwardedHeaders();
                
                app.UseExceptionHandler(exceptionHandlerApp =>
                {
                    exceptionHandlerApp.Run(async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = Text.Plain;

                        var feature = context.Features.Get<IExceptionHandlerFeature>();

                        await context.Response.WriteAsync(feature.Error.Message);
                    });
                });

                app.UseHsts();
            }

            var staticFileExtensionProvider = new FileExtensionContentTypeProvider();
            staticFileExtensionProvider.Mappings[".avif"] = "image/avif";
            
            var staticFileOptions = new StaticFileOptions
            {
                ContentTypeProvider = staticFileExtensionProvider,
                OnPrepareResponse = ctx =>
                {
                    if (ctx.File.Name == "index.html")
                    {
                        ctx.Context.Response.Headers["Cache-Control"] = new StringValues("no-cache, no-store");
                        ctx.Context.Response.Headers["Expires"] = new StringValues("-1");
                    }
                }
            };
            app.UseStaticFiles(staticFileOptions);

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health");
            app.MapControllerRoute("default", "api/{controller=Home}/{action=Index}/{id?}");
            app.MapHangfireDashboard("/hangfire", new DashboardOptions{Authorization = new[] {new MyAuthorizationFilter()}});
            app.MapFallbackToFile("index.html");
            
            // Get logger from the built app for Jobs configuration
            var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
            Jobs.ConfigureJobs(app, appLogger);

            app.Run();
        }
    }
}
