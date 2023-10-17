using System;
using System.Threading;
using System.Threading.Tasks;
using core.fs.Accounts;
using core.fs.Alerts;
using core.fs.Shared.Adapters.Brokerage;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices;
public class StopLossServiceHost : GenericBackgroundServiceHost
{
    private readonly IMarketHours _marketHours;
    private readonly MonitoringServices.StopLossMonitoringService _service;

    public StopLossServiceHost(
        ILogger<StopLossServiceHost> logger,
        IMarketHours marketHours,
        MonitoringServices.StopLossMonitoringService stopLossMonitoringService) : base(logger)
    {
        _marketHours = marketHours;
        _service = stopLossMonitoringService;
    }

    protected override TimeSpan GetSleepDuration() =>
        MonitoringServices.nextStopLossRun(DateTimeOffset.UtcNow, _marketHours) - DateTimeOffset.UtcNow;

    protected override async Task Loop(CancellationToken stoppingToken) => await _service.Execute(stoppingToken);
}

public class BrokerageServiceHost : GenericBackgroundServiceHost
{
    private readonly RefreshBrokerageConnectionService _service;

    public BrokerageServiceHost(ILogger<BrokerageServiceHost> logger, RefreshBrokerageConnectionService service) : base(logger)
    {
        _service = service;
    }

    protected override TimeSpan GetSleepDuration() => TimeSpan.FromHours(24);

    protected override Task Loop(CancellationToken stoppingToken)
    {
        return _service.Execute(stoppingToken);
    }
}
