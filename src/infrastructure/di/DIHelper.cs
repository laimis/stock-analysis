using core.fs;
using core.fs.Adapters.Authentication;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.Cryptos;
using core.fs.Adapters.CSV;
using core.fs.Adapters.Email;
using core.fs.Adapters.SEC;
using core.fs.Adapters.SMS;
using core.fs.Adapters.Storage;
using core.fs.Alerts;
using core.Shared;
using csvparser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using secedgar;
using securityutils;
using storage.shared;

namespace di
{
    public static class DIHelper
    {
        public static void RegisterServices(
            IConfiguration configuration,
            IServiceCollection services,
            ILogger logger)
        {
            services.AddSingleton<IRoleService>(_ => new RoleService(
                configuration.GetValue<string>("ADMINEmail")
            ));
            services.AddSingleton<core.fs.Adapters.Logging.ILogger, GenericLogger>();

            var marketCapToken = configuration.GetValue<string>("COINMARKETCAPToken");
            if (!string.IsNullOrEmpty(marketCapToken))
            {
                services.AddSingleton(s =>
                    new coinmarketcap.CoinMarketCapClient(
                        s.GetService<ILogger<coinmarketcap.CoinMarketCapClient>>(),
                        marketCapToken
                    )
                );
                services.AddSingleton<ICryptoService>(s => s.GetService<coinmarketcap.CoinMarketCapClient>()!);
            }

            services.AddSingleton<IPortfolioStorage, PortfolioStorage>();
            services.AddSingleton<ICSVParser, CSVParser>();
            services.AddSingleton<IMarketHours, timezonesupport.MarketHours>();
            services.AddSingleton<StockAlertContainer>();
            services.AddSingleton<IPasswordHashProvider, PasswordHashProvider>();
            services.AddSingleton<ICSVWriter, CsvWriterImpl>();
            services.AddSingleton<ISECFilings, EdgarClient>();

            // go over all types in core.fs assembly, find any inner classes and register any type that implements IApplicationService
            // interface as a singleton
            var markerInterface = typeof(IApplicationService);
            var assembly = typeof(core.fs.Options.OptionsHandler).Assembly;
            
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsInterface || type.IsAbstract)
                {
                    continue;
                }
            
                if (markerInterface.IsAssignableFrom(type))
                {
                    services.AddSingleton(type);
                }
            }

            services.AddSingleton<IEmailService>(s => 
                new sendgridclient.SendGridClientImpl(
                    configuration.GetValue<string>("SENDGRID_API_KEY"),
                    s.GetService<ILogger<sendgridclient.SendGridClientImpl>>()
                )
            );

            services.AddSingleton<ISMSClient>(s => 
                new twilioclient.TwilioClientWrapper(
                    configuration.GetValue<string>("TWILIO_ACCOUNT_SID"),
                    configuration.GetValue<string>("TWILIO_AUTH_TOKEN"),
                    configuration.GetValue<string>("TWILIO_FROM_NUMBER"),
                    s.GetService<ILogger<twilioclient.TwilioClientWrapper>>()!,
                    configuration.GetValue<string>("TWILIO_TO_NUMBER")));

            var schwabCallbackUrl = configuration.GetValue<string>("SCHWAB_CALLBACK_URL");
            
            if (schwabCallbackUrl != null)
            {
                services.AddSingleton<IBrokerage>(s =>
                    new SchwabClient.SchwabClient(
                        s.GetService<IBlobStorage>(),
                        schwabCallbackUrl,
                        configuration.GetValue<string>("SCHWAB_CLIENT_ID"),
                        configuration.GetValue<string>("SCHWAB_CLIENT_SECRET"),
                        new FSharpOption<ILogger<SchwabClient.SchwabClient>>(s.GetService<ILogger<SchwabClient.SchwabClient>>()!)
                    ));
            }
            else
            {
                logger.LogWarning("Dummy brokerage client registered, no brokerage callback url provided");
                // dummy brokerage client
                services.AddSingleton<IBrokerage>(_ => new DummyBrokerageClient());
            }
            
            StorageRegistrations(configuration, services, logger);
        }

        private static void StorageRegistrations(IConfiguration configuration, IServiceCollection services, ILogger logger)
        {
            services.AddSingleton<IOutbox, IncompleteOutbox>();
            
            var storage = configuration.GetValue<string>("storage");

            logger.LogInformation("Read storage configuration type: {storage}", storage);
            
            if (storage == "postgres")
            {
                RegisterPostgresImplementations(configuration, services);
            }
            else if (storage == "memory")
            {
                RegisterMemoryImplementations(services);
            }
            else
            {
                logger.LogCritical("Invalid storage configuration: {storage}", storage);

                throw new InvalidOperationException(
                    $"configuration 'storage' has value '{storage}', only 'postgres' or 'memory' is supported"
                );
            }
        }

        private static void RegisterMemoryImplementations(IServiceCollection services)
        {
            services.AddSingleton<IAccountStorage>(s =>
                new storage.memory.AccountStorage(s.GetRequiredService<IOutbox>())
            );

            services.AddSingleton<IAggregateStorage>(s =>
                new storage.memory.MemoryAggregateStorage(s.GetRequiredService<IOutbox>())
            );

            services.AddSingleton<IBlobStorage>(s =>
                new storage.memory.MemoryAggregateStorage(s.GetRequiredService<IOutbox>())
            );
        }

        private static void RegisterPostgresImplementations(IConfiguration configuration, IServiceCollection services)
        {
            var cnn = configuration.GetValue<string>("DB_CNN");
            if (string.IsNullOrEmpty(cnn))
            {
                throw new InvalidOperationException("DB_CNN configuration is required for postgres storage");
            }
            services.AddSingleton<IAccountStorage>(s => new storage.postgres.AccountStorage(s.GetRequiredService<IOutbox>(), cnn));
            services.AddSingleton<IBlobStorage>(s => new storage.postgres.PostgresAggregateStorage(s.GetRequiredService<IOutbox>(), cnn));
            services.AddSingleton<IAggregateStorage>(s => new storage.postgres.PostgresAggregateStorage(s.GetRequiredService<IOutbox>(), cnn));
        }
    }
}
