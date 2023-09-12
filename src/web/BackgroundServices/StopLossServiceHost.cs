using System;
using System.Threading;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Alerts;
using core.Alerts.Services;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Storage;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices;
public class StopLossServiceHost : GenericBackgroundServiceHost
{
    private IMarketHours _marketHours;
    private StopLossMonitoringService _service;

    public StopLossServiceHost(
        IAccountStorage accounts,
        IBrokerage brokerage,
        StockAlertContainer container,
        ILogger<StopLossServiceHost> logger,
        IMarketHours marketHours,
        IPortfolioStorage portfolioStorage) : base(logger)
    {
        _marketHours = marketHours;
        _service = new StopLossMonitoringService(accounts, brokerage, container, portfolioStorage);
    }

    

    protected override TimeSpan GetSleepDuration() => StopLossMonitoringService.CalculateNextRunDateTime(DateTimeOffset.UtcNow, _marketHours) - DateTimeOffset.UtcNow;

    protected override async Task Loop(CancellationToken stoppingToken) => await _service.Execute(stoppingToken);
}
