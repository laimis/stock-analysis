using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json.Serialization;
using Hangfire;
using web.Utils;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Primitives;

namespace web
{
    public class Startup
    {
        public Startup(
            IConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<Startup>();
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public ILogger Logger { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            AuthHelper.Configure(Configuration, services, Configuration.GetValue<string>("ADMINEmail"));
            
            services.AddHangfire(config =>
            {
                Console.WriteLine("what is this");
                config.UseDashboardMetrics();
            });
            services.AddHangfireServer();

            services
                .AddControllers(
                    o => {
                        o.InputFormatters.Add(new TextPlainInputFormatter());
                })
                .AddJsonOptions(o => {
                    
                    // go through this assembly types, and find all types that are JsonConverter<T>
                    // and add them to the options
                    foreach(var type in typeof(Startup).Assembly.GetTypes())
                    {
                        if (type.IsSubclassOf(typeof(JsonConverter)) && !type.IsAbstract)
                        {
                            var converter = Activator.CreateInstance(type);
                            o.JsonSerializerOptions.Converters.Add((JsonConverter)converter);
                        }
                    }
                });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddHealthChecks()
                .AddCheck<HealthCheck>("storage based health check");

            DIHelper.RegisterServices(Configuration, services, Logger);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllerRoute("default", "api/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapFallbackToFile("index.html");
            });

            app.UseHangfireDashboard();

            Jobs.ConfigureJobs(app, logger);
        }
    }
}
