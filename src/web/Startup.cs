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
using System.Threading;
using core.fs.Portfolio;
using Hangfire;
using Hangfire.PostgreSql;
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

            var configuration = app.ApplicationServices.GetService<IConfiguration>();
            
            var backendJobsSwitch = configuration.GetValue<string>("BACKEND_JOBS");
            if (backendJobsSwitch != "off")
            {
                logger.LogInformation("Backend jobs turned on");
                
                var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
                var rjo = new RecurringJobOptions
                {
                    TimeZone = tz
                };

                RecurringJob.AddOrUpdate<MonitoringServices.ThirtyDaySellService>(
                    recurringJobId: nameof(MonitoringServices.ThirtyDaySellService),
                    methodCall: service => service.Execute(),
                    cronExpression: Cron.Daily(9, 0),
                    options: rjo
                );
                
                RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.PatternMonitoringService>(
                    recurringJobId: nameof(core.fs.Alerts.MonitoringServices.PatternMonitoringService),
                    methodCall: service => service.Execute(),
                    cronExpression: "45 6-13 * * 1-5"
                );
                
                RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.StopLossMonitoringService>(
                    recurringJobId: nameof(core.fs.Alerts.MonitoringServices.StopLossMonitoringService),
                    methodCall: service => service.Execute(),
                    cronExpression: "*/5 6-13 * * 1-5",
                    options: rjo
                );
                
                RecurringJob.AddOrUpdate<core.fs.Brokerage.MonitoringServices.AccountMonitoringService>(
                    recurringJobId: nameof(core.fs.Brokerage.MonitoringServices.AccountMonitoringService),
                    methodCall: service => service.Execute(),
                    cronExpression: "0 15 * * *",
                    options: rjo
                );
                
                RecurringJob.AddOrUpdate<core.fs.Accounts.RefreshBrokerageConnectionService>(
                    recurringJobId: nameof(core.fs.Accounts.RefreshConnection),
                    methodCall: service => service.Execute(),
                    cronExpression: "0 20 * * *",
                    options: rjo
                );
                
                var multipleExpressions = new[] { "50 6 * * 1-5", "20 14 * * 1-5" };
                
                foreach (var exp in multipleExpressions)
                {
                    RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.AlertEmailService>(
                        recurringJobId: nameof(core.fs.Alerts.MonitoringServices.AlertEmailService),
                        methodCall: service => service.Execute(),
                        cronExpression: exp,
                        options: rjo
                    );
                }
                
                RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.WeeklyMonitoringService>(
                    recurringJobId: nameof(core.fs.Alerts.MonitoringServices.WeeklyMonitoringService),
                    methodCall: service => service.Execute(false),
                    cronExpression: "0 10 * * 6",
                    options: rjo
                );
            }
            else
            {
                logger.LogInformation("Backend jobs turned off");
            }
        }
    }
}
