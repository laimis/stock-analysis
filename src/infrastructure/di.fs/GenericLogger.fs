namespace di

open Microsoft.Extensions.Logging

type GenericLogger(logger: ILogger<GenericLogger>) =
    
    interface core.fs.Adapters.Logging.ILogger with
        member _.LogInformation(message: string) = 
            logger.LogInformation(message)
        
        member _.LogWarning(message: string) = 
            logger.LogWarning(message)
        
        member _.LogError(message: string) = 
            logger.LogError(message)

type WrappingLogger(logger: ILogger) =
    
    interface core.fs.Adapters.Logging.ILogger with
        member _.LogInformation(message: string) = 
            logger.LogInformation(message)
        
        member _.LogWarning(message: string) = 
            logger.LogWarning(message)
        
        member _.LogError(message: string) = 
            logger.LogError(message)
