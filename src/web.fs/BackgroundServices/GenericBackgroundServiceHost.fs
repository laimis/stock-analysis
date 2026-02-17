namespace web.BackgroundServices

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting

[<AbstractClass>]
type GenericBackgroundServiceHost(logger: core.fs.Adapters.Logging.ILogger) =
    inherit BackgroundService()
    
    abstract GetNextRunDateTime: referenceTime: DateTimeOffset -> DateTimeOffset
    
    abstract Loop: logger: core.fs.Adapters.Logging.ILogger * stoppingToken: CancellationToken -> Task
    
    override this.ExecuteAsync(stoppingToken: CancellationToken) = task {
        // warm up sleep duration, generate random seconds from 5 to 10 seconds
        let random = Random()
        let randomSleep = random.Next(5, 10)
        do! Task.Delay(TimeSpan.FromSeconds(float randomSleep), stoppingToken)
        
        logger.LogInformation($"running {this.GetType().Name}")
        
        while not stoppingToken.IsCancellationRequested do
            try
                do! this.Loop(logger, stoppingToken)
            with
            | ex -> logger.LogError($"Failed: {ex}")
            
            let now = DateTimeOffset.UtcNow
            let nextRun = this.GetNextRunDateTime(now)
            let sleepDuration = nextRun.Subtract(now)
            
            match sleepDuration.TotalMinutes with
            | minutes when minutes > 1.0 ->
                logger.LogInformation($"Next run {nextRun:u}, sleeping for {sleepDuration}")
            | minutes when minutes < 0.0 ->
                logger.LogWarning($"sleep duration is negative: {sleepDuration}")
            | _ -> ()
            
            do! Task.Delay(sleepDuration, stoppingToken)
        
        logger.LogInformation($"{this.GetType().Name} exit")
    }
