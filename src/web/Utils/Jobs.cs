using System;
using core.fs.Portfolio;
using Hangfire;
using Hangfire.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace web.Utils;

public static class Jobs
{
    public static void ConfigureJobs(IApplicationBuilder app, ILogger<Startup> logger)
    {
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
            BackgroundJob.Schedule<core.fs.Alerts.MonitoringServices.PatternMonitoringService>(
                service => service.Execute(),
                TimeSpan.FromMinutes(1)
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
