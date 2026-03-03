module web.Jobs

open Hangfire
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open core.fs.Portfolio.MonitoringServices
open core.fs.Alerts.MonitoringServices
open core.fs.Options.MonitoringServices
open secedgar.fs
open core.fs.Alerts.SECFilingsMonitoring
open core.fs.Brokerage.MonitoringServices
open core.fs.Accounts
open core.fs.Services.SECTickerSyncService
open core.fs.Reports
open core.fs.Admin

let backendJobsEnabled (configuration: IConfiguration) =
    let backendJobsSwitch = configuration.GetValue<string> "BACKEND_JOBS"
    backendJobsSwitch <> "off"

let addJobs (configuration: IConfiguration) (services: IServiceCollection) (logger: ILogger) =
    services.AddHangfire(fun config ->
        config.UseDashboardMetrics() |> ignore
    ) |> ignore 

    match backendJobsEnabled configuration with
    | false -> 
        logger.LogInformation "Jobs: backend jobs are disabled"
    | true ->
        logger.LogInformation "Jobs: backend jobs are enabled"
        services.AddHangfireServer(fun (options:BackgroundJobServerOptions) ->
            options.WorkerCount <- 2
        ) |> ignore

let configureJobs (app: IApplicationBuilder) (logger: ILogger) =

    let configuration = app.ApplicationServices.GetService<IConfiguration>()

    match backendJobsEnabled configuration with
    | false -> logger.LogInformation "Jobs: backend jobs are disabled"
    | true ->
        logger.LogInformation "Jobs: backend jobs are enabled"
    
        let pacificTmz = System.TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles")
        let rjo = new RecurringJobOptions(TimeZone = pacificTmz)

        RecurringJob.AddOrUpdate<PortfolioAnalysisService>("ThirtyDayTransactions", (fun (s:PortfolioAnalysisService) -> s.ReportOnThirtyDayTransactions() |> ignore), Cron.Daily(9, 0), rjo)
        
        RecurringJob.AddOrUpdate<PatternMonitoringService>("PatternMonitoring", (fun (s:PatternMonitoringService) -> s.RunPatternMonitoring() |> ignore), "45 6-13 * * 1-5", rjo)
        BackgroundJob.Schedule<PatternMonitoringService>((fun (s:PatternMonitoringService) -> s.RunPatternMonitoring() |> ignore), System.TimeSpan.FromMinutes 2.0) |> ignore
        
        RecurringJob.AddOrUpdate<PriceObvTrendMonitoringService>("PriceObvTrendMonitoring", (fun (s:PriceObvTrendMonitoringService) -> s.Run() |> ignore), "55 13 * * 1-5", rjo)
        
        RecurringJob.AddOrUpdate<PriceMonitoringService>("PriceMonitoring", (fun (s:PriceMonitoringService) -> s.Run() |> ignore), "35 6-14 * * 1-5", rjo)
        
        RecurringJob.AddOrUpdate<ExpirationMonitoringService>("ExpirationMonitoring", (fun (s:ExpirationMonitoringService) -> s.Run() |> ignore), "5 16 * * 1-5", rjo)
        
        RecurringJob.AddOrUpdate<StopLossMonitoringService>("StopLossMonitoring", (fun (s:StopLossMonitoringService) -> s.RunStopLossMonitoring() |> ignore), "*/5 6-13 * * 1-5", rjo)
        
        RecurringJob.AddOrUpdate<PriceAlertMonitoringService>("PriceAlertMonitoring", (fun (s:PriceAlertMonitoringService) -> s.Execute() |> ignore), "*/5 6-13 * * 1-5", rjo)
        
        RecurringJob.AddOrUpdate<PriceAlertNearTriggerMonitoringService>("PriceAlertNearTriggerMonitoring", (fun (s:PriceAlertNearTriggerMonitoringService) -> s.Execute() |> ignore), "0 14 * * 1-5", rjo)
        
        RecurringJob.AddOrUpdate<SECFilingsSyncService>("SECFilingsSyncService", (fun (s:SECFilingsSyncService) -> s.Execute() |> ignore), "*/10 6-19 * * 1-5", rjo)
        
        RecurringJob.AddOrUpdate<SECFilingsMonitoringService>("SECFilingsMonitoringService", (fun (s:SECFilingsMonitoringService) -> s.Execute() |> ignore), "*/30 6-19 * * 1-5", rjo)

        RecurringJob.AddOrUpdate<Schedule13GProcessingService>("Schedule13GProcessingService", (fun (s:Schedule13GProcessingService) -> s.Execute() |> ignore), "0 9 * * 1-5", rjo)
        
        RecurringJob.AddOrUpdate<Form144ProcessingService>("Form144ProcessingService", (fun (s:Form144ProcessingService) -> s.Execute() |> ignore), "30 9 * * 1-5", rjo)
        
        RecurringJob.AddOrUpdate<Schedule13DProcessingService>("Schedule13DProcessingService", (fun (s:Schedule13DProcessingService) -> s.Execute() |> ignore), "45 9 * * 1-5", rjo)
        
        RecurringJob.AddOrUpdate<ReminderMonitoringService>("ReminderMonitoringService", (fun (s:ReminderMonitoringService) -> s.Execute() |> ignore), "0 6 * * *", rjo)
            
        RecurringJob.AddOrUpdate<AccountMonitoringService>("AccountMonitoringService", (fun (s:AccountMonitoringService) -> s.RunAccountValueOrderAndTransactionSync() |> ignore), Cron.Daily(15), rjo)
        
        RecurringJob.AddOrUpdate<AccountMonitoringService>("AccountMonitoringServiceProcessTransactions", (fun (s:AccountMonitoringService) -> s.RunTransactionProcessing() |> ignore), Cron.Daily(15, 10), rjo)
            
        RecurringJob.AddOrUpdate<RefreshBrokerageConnectionService>("RefreshBrokerageConnectionService", (fun (s:RefreshBrokerageConnectionService) -> s.Execute() |> ignore), Cron.Daily(20), rjo)
        
        // 6:50am and 2:20pm
        [| "50 6 * * 1-5"; "20 14 * * 1-5" |]
        |> Array.iteri (fun i exp ->
            RecurringJob.AddOrUpdate<AlertEmailService>(nameof(AlertEmailService) + i.ToString(), (fun (s:AlertEmailService) -> s.Execute() |> ignore), exp, rjo)
        )

        RecurringJob.AddOrUpdate<WeeklyMonitoringService>("WeeklyMonitoringService", (fun (s:WeeklyMonitoringService) -> s.Execute(false) |> ignore), Cron.Weekly(System.DayOfWeek.Saturday, 10), rjo)

        RecurringJob.AddOrUpdate<SECTickerSyncService>("SECTickerSyncService", (fun (s:SECTickerSyncService) -> s.Execute() |> ignore), Cron.Weekly(System.DayOfWeek.Saturday, 9), rjo)

        RecurringJob.AddOrUpdate<SummaryEmailService>("WeeklySummaryService", (fun (s:SummaryEmailService) -> s.ExecuteWeekly() |> ignore), Cron.Weekly(System.DayOfWeek.Friday, 18), rjo)

        RecurringJob.AddOrUpdate<SummaryEmailService>("MonthlySummaryService", (fun (s:SummaryEmailService) -> s.ExecuteMonthly() |> ignore), Cron.Monthly(1, 6), rjo)

        RecurringJob.AddOrUpdate<PortfolioAnalysisService>("PortfolioAnalysisServiceReportOnMaxProfitBasedOnDaysHeld", (fun (s:PortfolioAnalysisService) -> s.ReportOnMaxProfitBasedOnDaysHeld() |> ignore), Cron.Weekly(System.DayOfWeek.Saturday, 8), rjo)
        
        RecurringJob.AddOrUpdate<PortfolioAnalysisService>("PortfolioAnalysisServiceRecentlyClosedPositionUpdates", (fun (s:PortfolioAnalysisService) -> s.RecentlyClosedPositionUpdates() |> ignore), Cron.Daily(20, 0), rjo)

        RecurringJob.AddOrUpdate<AdminCleanUpService>("AdminCleanUpService", (fun (s:AdminCleanUpService) -> s.Execute() |> ignore), Cron.Daily 2, rjo)