namespace web.Utils

open System
open Hangfire
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

module Jobs =
    
    let private backendJobsEnabled (configuration: IConfiguration) =
        let backendJobsSwitch = configuration.GetValue<string>("BACKEND_JOBS")
        backendJobsSwitch <> "off"
    
    let configureJobs (app: IApplicationBuilder) (logger: ILogger<web.Program>) =
        let configuration = app.ApplicationServices.GetService<IConfiguration>()
        
        if backendJobsEnabled configuration then
            logger.LogInformation("Backend jobs turned on")
            
            let pacificTmz = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles")
            let rjo = RecurringJobOptions(TimeZone = pacificTmz)
            
            RecurringJob.AddOrUpdate<core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService>(
                recurringJobId = nameof(core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService.ReportOnThirtyDayTransactions),
                methodCall = (fun (service: core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService) -> service.ReportOnThirtyDayTransactions()),
                cronExpression = Cron.Daily(9, 0),
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.PatternMonitoringService>(
                recurringJobId = nameof(core.fs.Alerts.MonitoringServices.PatternMonitoringService),
                methodCall = (fun (service: core.fs.Alerts.MonitoringServices.PatternMonitoringService) -> service.RunPatternMonitoring()),
                cronExpression = "45 6-13 * * 1-5",
                options = rjo // 6:45am to 1:45pm Monday through Friday
            ) |> ignore
            
            BackgroundJob.Schedule<core.fs.Alerts.MonitoringServices.PatternMonitoringService>(
                (fun (service: core.fs.Alerts.MonitoringServices.PatternMonitoringService) -> service.RunPatternMonitoring()),
                TimeSpan.FromMinutes(2)
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.PriceObvTrendMonitoringService>(
                recurringJobId = nameof(core.fs.Alerts.MonitoringServices.PriceObvTrendMonitoringService),
                methodCall = (fun (service: core.fs.Alerts.MonitoringServices.PriceObvTrendMonitoringService) -> service.Run()),
                cronExpression = "55 13 * * 1-5", // 1:55pm Monday through Friday
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Options.MonitoringServices.PriceMonitoringService>(
                recurringJobId = nameof(core.fs.Options.MonitoringServices.PriceMonitoringService),
                methodCall = (fun (service: core.fs.Options.MonitoringServices.PriceMonitoringService) -> service.Run()),
                cronExpression = "35 6-14 * * 1-5", // 6:35am to 2:35pm Monday through Friday
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Options.MonitoringServices.ExpirationMonitoringService>(
                recurringJobId = nameof(core.fs.Options.MonitoringServices.ExpirationMonitoringService),
                methodCall = (fun (service: core.fs.Options.MonitoringServices.ExpirationMonitoringService) -> service.Run()),
                cronExpression = "5 16 * * 1-5", // 4:05pm Monday through Friday (after market close)
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.StopLossMonitoringService>(
                recurringJobId = nameof(core.fs.Alerts.MonitoringServices.StopLossMonitoringService),
                methodCall = (fun (service: core.fs.Alerts.MonitoringServices.StopLossMonitoringService) -> service.RunStopLossMonitoring()),
                cronExpression = "*/5 6-13 * * 1-5", // every 5 minutes from 6am to 1pm
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.PriceAlertMonitoringService>(
                recurringJobId = nameof(core.fs.Alerts.MonitoringServices.PriceAlertMonitoringService),
                methodCall = (fun (service: core.fs.Alerts.MonitoringServices.PriceAlertMonitoringService) -> service.Execute()),
                cronExpression = "*/5 6-13 * * 1-5", // every 5 minutes from 6am to 1pm
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.PriceAlertNearTriggerMonitoringService>(
                recurringJobId = nameof(core.fs.Alerts.MonitoringServices.PriceAlertNearTriggerMonitoringService),
                methodCall = (fun (service: core.fs.Alerts.MonitoringServices.PriceAlertNearTriggerMonitoringService) -> service.Execute()),
                cronExpression = "0 14 * * 1-5", // 2pm ET weekdays after market close
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Alerts.SECFilingsMonitoring.SECFilingsMonitoringService>(
                recurringJobId = nameof(core.fs.Alerts.SECFilingsMonitoring.SECFilingsMonitoringService),
                methodCall = (fun (service: core.fs.Alerts.SECFilingsMonitoring.SECFilingsMonitoringService) -> service.Execute()),
                cronExpression = "30 8 * * 1-5", // 8:30am PT (11:30am ET) weekdays, couple hours after market opens
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.ReminderMonitoringService>(
                recurringJobId = nameof(core.fs.Alerts.MonitoringServices.ReminderMonitoringService),
                methodCall = (fun (service: core.fs.Alerts.MonitoringServices.ReminderMonitoringService) -> service.Execute()),
                cronExpression = "0 6 * * *", // 6am daily
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Brokerage.MonitoringServices.AccountMonitoringService>(
                recurringJobId = nameof(core.fs.Brokerage.MonitoringServices.AccountMonitoringService),
                methodCall = (fun (service: core.fs.Brokerage.MonitoringServices.AccountMonitoringService) -> service.RunAccountValueOrderAndTransactionSync()),
                cronExpression = Cron.Daily(15), // 3pm
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Brokerage.MonitoringServices.AccountMonitoringService>(
                recurringJobId = nameof(core.fs.Brokerage.MonitoringServices.AccountMonitoringService) + "ProcessTransactions",
                methodCall = (fun (service: core.fs.Brokerage.MonitoringServices.AccountMonitoringService) -> service.RunTransactionProcessing()),
                cronExpression = Cron.Daily(hour = 15, minute = 10), // 3:10pm
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Accounts.RefreshBrokerageConnectionService>(
                recurringJobId = nameof(core.fs.Accounts.RefreshConnection),
                methodCall = (fun (service: core.fs.Accounts.RefreshBrokerageConnectionService) -> service.Execute()),
                cronExpression = Cron.Daily(20), // 8pm
                options = rjo
            ) |> ignore
            
            // 6:50am and 2:20pm
            let multipleExpressions = [| "50 6 * * 1-5"; "20 14 * * 1-5" |]
            
            for exp in multipleExpressions do
                RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.AlertEmailService>(
                    recurringJobId = nameof(core.fs.Alerts.MonitoringServices.AlertEmailService),
                    methodCall = (fun (service: core.fs.Alerts.MonitoringServices.AlertEmailService) -> service.Execute()),
                    cronExpression = exp,
                    options = rjo
                ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Alerts.MonitoringServices.WeeklyMonitoringService>(
                recurringJobId = nameof(core.fs.Alerts.MonitoringServices.WeeklyMonitoringService),
                methodCall = (fun (service: core.fs.Alerts.MonitoringServices.WeeklyMonitoringService) -> service.Execute(false)),
                cronExpression = Cron.Weekly(DayOfWeek.Saturday, 10),
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Services.SECTickerSyncService.SECTickerSyncService>(
                recurringJobId = nameof(core.fs.Services.SECTickerSyncService.SECTickerSyncService),
                methodCall = (fun (service: core.fs.Services.SECTickerSyncService.SECTickerSyncService) -> service.Execute()),
                cronExpression = Cron.Weekly(DayOfWeek.Saturday, 9), // 9am Saturday
                options = rjo
            ) |> ignore
            
            RecurringJob.AddOrUpdate<core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService>(
                recurringJobId = nameof(core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService.ReportOnMaxProfitBasedOnDaysHeld),
                methodCall = (fun (service: core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService) -> service.ReportOnMaxProfitBasedOnDaysHeld()),
                cronExpression = Cron.Weekly(DayOfWeek.Saturday, 8),
                options = rjo
            ) |> ignore
            
            // run every day at night a job in portfolio analysis service
            // that will run recently closed positions updates
            RecurringJob.AddOrUpdate<core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService>(
                recurringJobId = nameof(core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService.RecentlyClosedPositionUpdates),
                methodCall = (fun (service: core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService) -> service.RecentlyClosedPositionUpdates()),
                cronExpression = Cron.Daily(20, 0),
                options = rjo
            ) |> ignore
        else
            logger.LogInformation("Backend jobs turned off")
    
    let addJobs (configuration: IConfiguration) (services: IServiceCollection) (logger: ILogger) =
        services.AddHangfire(fun config ->
            config.UseDashboardMetrics() |> ignore
        ) |> ignore
        
        if backendJobsEnabled configuration then
            services.AddHangfireServer(fun s ->
                s.WorkerCount <- 2
            ) |> ignore
        else
            logger.LogInformation("Backend jobs turned off")
