using Microsoft.Extensions.Logging;
using ILogger = core.fs.Adapters.Logging.ILogger;

namespace di;

public class GenericLogger : ILogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    public GenericLogger(ILogger<GenericLogger> logger)
    {
        _logger = logger;
    }

    public void LogInformation(string message) => _logger.LogInformation(message);
    
    public void LogWarning(string message) => _logger.LogWarning(message);

    public void LogError(string message) => _logger.LogError(message);
}

public class WrappingLogger : ILogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    public WrappingLogger(Microsoft.Extensions.Logging.ILogger logger)
    {
        _logger = logger;
    }

    public void LogInformation(string message) => _logger.LogInformation(message);

    public void LogWarning(string message) => _logger.LogWarning(message);

    public void LogError(string message) => _logger.LogError(message);
}
