namespace di

open System
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open core.fs
open core.fs.Adapters.Authentication
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Cryptos
open core.fs.Adapters.CSV
open core.fs.Adapters.Email
open core.fs.Adapters.SEC
open core.fs.Adapters.SMS
open core.fs.Adapters.Storage
open core.fs.Alerts
open core.Shared
open storage.shared
open storage.postgres

module DIHelper =
    
    let private storageRegistrations (configuration: IConfiguration) (services: IServiceCollection) (logger: ILogger) =
        services.AddSingleton<IOutbox, IncompleteOutbox>() |> ignore
        
        let storage = configuration.GetValue<string>("storage")
        
        logger.LogInformation("Read storage configuration type: {storage}", storage)
        
        if storage = "postgres" then
            let cnn = configuration.GetValue<string>("DB_CNN")
            
            if String.IsNullOrEmpty(cnn) then
                raise (InvalidOperationException("DB_CNN configuration is required for postgres storage"))
            
            services.AddSingleton<IAccountStorage>(fun sp ->
                let outbox = sp.GetRequiredService<IOutbox>()
                AccountStorage(outbox, cnn) :> IAccountStorage
            ) |> ignore
            
            services.AddSingleton<IBlobStorage>(fun sp ->
                let outbox = sp.GetRequiredService<IOutbox>()
                PostgresAggregateStorage(outbox, cnn) :> IBlobStorage
            ) |> ignore
            
            services.AddSingleton<IAggregateStorage>(fun sp ->
                let outbox = sp.GetRequiredService<IOutbox>()
                PostgresAggregateStorage(outbox, cnn) :> IAggregateStorage
            ) |> ignore
            
            services.AddSingleton<ISECFilingStorage>(fun _ ->
                SECFilingStorage(cnn) :> ISECFilingStorage
            ) |> ignore
            
            services.AddSingleton<IOwnershipStorage>(fun _ ->
                OwnershipStorage(cnn) :> IOwnershipStorage
            ) |> ignore
        else
            logger.LogCritical("Invalid storage configuration: {storage}", storage)
            raise (InvalidOperationException(
                sprintf "configuration 'storage' has value '%s'. Only 'postgres' is supported. Please set the 'storage' environment variable to 'postgres' and ensure DB_CNN is configured." storage
            ))
    
    let registerServices (configuration: IConfiguration) (services: IServiceCollection) (logger: ILogger) =
        
        // Register role service
        let adminEmail = configuration.GetValue<string>("ADMINEmail")
        services.AddSingleton<IRoleService>(fun _ -> 
            RoleService(if String.IsNullOrWhiteSpace(adminEmail) then None else Some adminEmail) :> IRoleService
        ) |> ignore
        
        // Register generic logger
        services.AddSingleton<core.fs.Adapters.Logging.ILogger, GenericLogger>() |> ignore
        
        // Register CoinMarketCap client
        let marketCapToken = configuration.GetValue<string>("COINMARKETCAPToken")
        services.AddSingleton<ICryptoService>(fun sp ->
            let loggerService = sp.GetService<ILogger<coinmarketcap.CoinMarketCapClient>>()
            let loggerOption = 
                if loggerService <> null then Some loggerService else None
            
            coinmarketcap.CoinMarketCapClient(
                loggerOption,
                (if String.IsNullOrEmpty(marketCapToken) then "notset" else marketCapToken)
            ) :> ICryptoService
        ) |> ignore
        
        // Register infrastructure services
        services.AddSingleton<IPortfolioStorage, PortfolioStorage>() |> ignore
        services.AddSingleton<ICSVParser, csvparser.fs.CSVParser>() |> ignore
        services.AddSingleton<IMarketHours, timezonesupport.MarketHours>() |> ignore
        services.AddSingleton<StockAlertContainer>() |> ignore
        services.AddSingleton<IPasswordHashProvider, securityutils.PasswordHashProvider>() |> ignore
        services.AddSingleton<ICSVWriter, csvparser.fs.CsvWriterImpl>() |> ignore
        
        // Register SEC client
        services.AddSingleton<ISECFilings>(fun sp ->
            let logger = sp.GetService<ILogger<secedgar.fs.EdgarClient>>()
            let accountStorage = sp.GetService<IAccountStorage>()
            secedgar.fs.EdgarClient(
                Some logger,
                Some accountStorage
            ) :> ISECFilings
        ) |> ignore
        
        // Register all IApplicationService implementations from core.fs assembly
        let markerInterface = typeof<IApplicationService>
        let coreAssembly = typeof<core.fs.Options.OptionsHandler>.Assembly
        
        for t in coreAssembly.GetTypes() do
            if not t.IsInterface && not t.IsAbstract && markerInterface.IsAssignableFrom(t) then
                services.AddSingleton(t) |> ignore
        
        // Register all IApplicationService implementations from secedgar.fs assembly
        let secedgarAssembly = typeof<secedgar.fs.Schedule13GProcessingService>.Assembly
        
        for t in secedgarAssembly.GetTypes() do
            if not t.IsInterface && not t.IsAbstract && markerInterface.IsAssignableFrom(t) then
                services.AddSingleton(t) |> ignore
        
        // Register email service
        services.AddSingleton<IEmailService>(fun _ ->
            emailclient.SESEmailService(
                (configuration.GetValue<string>("AWS_ACCESS_KEY_ID") |> Option.ofObj |> Option.defaultValue "notset"),
                (configuration.GetValue<string>("AWS_SECRET_ACCESS_KEY") |> Option.ofObj |> Option.defaultValue "notset"),
                (configuration.GetValue<string>("AWS_REGION") |> Option.ofObj |> Option.defaultValue "us-east-1")
            ) :> IEmailService
        ) |> ignore
        
        // Register Twilio client
        services.AddSingleton<ISMSClient>(fun sp ->
            let accountSid = configuration.GetValue<string>("TWILIO_ACCOUNT_SID")
            let authToken = configuration.GetValue<string>("TWILIO_AUTH_TOKEN")
            let fromNumber = configuration.GetValue<string>("TWILIO_FROM_NUMBER")
            let toNumber = configuration.GetValue<string>("TWILIO_TO_NUMBER")
            let logger = sp.GetService<ILogger<twilioclient.TwilioClientWrapper>>()
            
            twilioclient.TwilioClientWrapper(
                (if String.IsNullOrEmpty(accountSid) then None else Some accountSid),
                (if String.IsNullOrEmpty(authToken) then None else Some authToken),
                (if String.IsNullOrEmpty(fromNumber) then None else Some fromNumber),
                logger,
                (if String.IsNullOrEmpty(toNumber) then None else Some toNumber)
            ) :> ISMSClient
        ) |> ignore
        
        // Register Schwab brokerage client or dummy
        let schwabCallbackUrl = configuration.GetValue<string>("SCHWAB_CALLBACK_URL")
        
        if schwabCallbackUrl <> null then
            services.AddSingleton<IBrokerage>(fun sp ->
                let blobStorage = sp.GetService<IBlobStorage>()
                let clientId = configuration.GetValue<string>("SCHWAB_CLIENT_ID")
                let clientSecret = configuration.GetValue<string>("SCHWAB_CLIENT_SECRET")
                let logger = sp.GetService<ILogger<SchwabClient.SchwabClient>>()
                
                SchwabClient.SchwabClient(
                    blobStorage,
                    schwabCallbackUrl,
                    clientId,
                    clientSecret,
                    (if logger <> null then Some logger else None)
                ) :> IBrokerage
            ) |> ignore
        else
            logger.LogWarning("Dummy brokerage client registered, no brokerage callback url provided")
            services.AddSingleton<IBrokerage>(fun _ -> DummyBrokerageClient() :> IBrokerage) |> ignore
        
        // Register storage
        storageRegistrations configuration services logger
