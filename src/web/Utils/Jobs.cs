using System;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace web.Utils;

public static class Jobs
{
    public static void ConfigureJobs(IApplicationBuilder app, ILogger<Program> logger)
    {
        var configuration = app.ApplicationServices.GetService<IConfiguration>();
        
        if (BackendJobsEnabled(configuration))
        {
            logger.LogInformation("Backend jobs turned on");
            
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
            var rjo = new RecurringJobOptions
            {
                TimeZone = tz
            };

            RecurringJob.AddOrUpdate<core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService>(
                recurringJobId: nameof(core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService.ReportOnThirtyDayTransactions),
                methodCall: service => service.ReportOnThirtyDayTransactions(),
                cronExpression: Cron.Daily(9, 0),
                options: rjo
            );
            
            RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.PatternMonitoringService>(
                recurringJobId: nameof(core.fs.Alerts.MonitoringServices.PatternMonitoringService),
                methodCall: service => service.RunPatternMonitoring(),
                cronExpression: "45 6-13 * * 1-5",
                options: rjo// 6:45am to 1:45pm Monday through Friday,
            );
            BackgroundJob.Schedule<core.fs.Alerts.MonitoringServices.PatternMonitoringService>(
                service => service.RunPatternMonitoring(),
                TimeSpan.FromMinutes(2)
            );

            RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.PriceObvTrendMonitoringService>(
                recurringJobId: nameof(core.fs.Alerts.MonitoringServices.PriceObvTrendMonitoringService),
                methodCall: service => service.Run(),
                cronExpression: "55 13 * * 1-5", //  1:55pm Monday through Friday
                options: rjo
            );
            
            RecurringJob.AddOrUpdate<core.fs.Options.MonitoringServices.PriceMonitoringService>(
                recurringJobId: nameof(core.fs.Options.MonitoringServices.PriceMonitoringService),
                methodCall: service => service.Run(),
                cronExpression: "35 6-14 * * 1-5", // 6:35am to 2:35pm Monday through Friday
                options: rjo
            );
            
            RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.StopLossMonitoringService>(
                recurringJobId: nameof(core.fs.Alerts.MonitoringServices.StopLossMonitoringService),
                methodCall: service => service.RunStopLossMonitoring(),
                cronExpression: "*/5 6-13 * * 1-5",  // every 5 minutes from 6am to 1pm
                options: rjo
            );
            
            RecurringJob.AddOrUpdate<core.fs.Brokerage.MonitoringServices.AccountMonitoringService>(
                recurringJobId: nameof(core.fs.Brokerage.MonitoringServices.AccountMonitoringService),
                methodCall: service => service.RunAccountValueOrderAndTransactionSync(),
                cronExpression: Cron.Daily(15), // 3pm
                options: rjo
            );
            
            RecurringJob.AddOrUpdate<core.fs.Brokerage.MonitoringServices.AccountMonitoringService>(
                recurringJobId: nameof(core.fs.Brokerage.MonitoringServices.AccountMonitoringService) + "ProcessTransactions",
                methodCall: service => service.RunTransactionProcessing(),
                cronExpression: Cron.Daily(hour: 15, minute: 10), // 3:10pm
                options: rjo
            );
            
            RecurringJob.AddOrUpdate<core.fs.Accounts.RefreshBrokerageConnectionService>(
                recurringJobId: nameof(core.fs.Accounts.RefreshConnection),
                methodCall: service => service.Execute(),
                cronExpression: Cron.Daily(20), // 8pm
                options: rjo
            );
            
            // 6:50am and 2:20pm
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
                cronExpression: Cron.Weekly(DayOfWeek.Saturday, 10),
                options: rjo
            );

            RecurringJob.AddOrUpdate<core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService>(
                recurringJobId: nameof(core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService.ReportOnMaxProfitBasedOnDaysHeld),
                methodCall: service => service.ReportOnMaxProfitBasedOnDaysHeld(),
                cronExpression: Cron.Weekly(DayOfWeek.Saturday, 8),
                options: rjo);
            
            // run every day at night a job in portfolio analysis service
            // that will run recently closed positions updates
            RecurringJob.AddOrUpdate<core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService>(
                recurringJobId: nameof(core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService.RecentlyClosedPositionUpdates),
                methodCall: service => service.RecentlyClosedPositionUpdates(),
                cronExpression: Cron.Daily(20, 0),
                options: rjo
            );
        }
        else
        {
            logger.LogInformation("Backend jobs turned off");
        }
    }

    private static bool BackendJobsEnabled(IConfiguration configuration)
    {
        var backendJobsSwitch = configuration.GetValue<string>("BACKEND_JOBS");
        return backendJobsSwitch != "off";
    }

    public static void AddJobs(IConfiguration configuration, IServiceCollection services, ILogger logger)
    {
        services.AddHangfire(config =>
        {
            config.UseDashboardMetrics();
        });

        if (BackendJobsEnabled(configuration))
        {
            services.AddHangfireServer(
                s => s.WorkerCount = 2
            );
        }
        else
        {
            logger.LogInformation("Backend jobs turned off");
        }
    }
}
