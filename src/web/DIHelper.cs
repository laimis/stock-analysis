using System;
using core.fs;
using core.fs.Adapters.Authentication;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.Cryptos;
using core.fs.Adapters.CSV;
using core.fs.Adapters.Email;
using core.fs.Adapters.SEC;
using core.fs.Adapters.SMS;
using core.fs.Adapters.Storage;
using core.fs.Adapters.Subscriptions;
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
using web.BackgroundServices;
using web.Utils;

namespace web
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
            services.AddSingleton(s =>
                new coinmarketcap.CoinMarketCapClient(
                    s.GetService<ILogger<coinmarketcap.CoinMarketCapClient>>(),
                    configuration.GetValue<string>("COINMARKETCAPToken")
                )
            );

            services.AddSingleton<ICryptoService>(s => s.GetService<coinmarketcap.CoinMarketCapClient>());
            services.AddSingleton<IPortfolioStorage, PortfolioStorage>();
            services.AddSingleton<ICSVParser, CSVParser>();
            services.AddSingleton<IMarketHours, timezonesupport.MarketHours>();
            services.AddSingleton<StockAlertContainer>();
            services.AddSingleton<CookieEvents>();
            services.AddSingleton<IPasswordHashProvider, PasswordHashProvider>();
            services.AddSingleton<ICSVWriter, CsvWriterImpl>();
            services.AddSingleton<ISECFilings, EdgarClient>();

            // go over all types in core.fs assembly, find any inner classes and register any type that implements IApplicationService
            // interface as a singleton
            var markerInterface = typeof(IApplicationService);
            var assembly = typeof(core.fs.Options.Handler).Assembly;
            
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
            
            services.AddSingleton<ISubscriptions>(s => 
                new stripe.Subscriptions(
                    configuration.GetValue<string>("STRIPE_API_KEY")
                )
            );

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
                    s.GetService<ILogger<twilioclient.TwilioClientWrapper>>(),
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
                        new FSharpOption<ILogger<SchwabClient.SchwabClient>>(s.GetService<ILogger<SchwabClient.SchwabClient>>())
                    ));
            }
            else
            {
                logger.LogWarning("Dummy brokerage client registered, no brokerage callback url provided");
                // dummy brokerage client
                services.AddSingleton<IBrokerage>(s => new DummyBrokerageClient());
            }
            
            StorageRegistrations(configuration, services, logger);
            
            var backendJobsSwitch = configuration.GetValue<string>("BACKEND_JOBS");
            if (backendJobsSwitch != "off")
            {
                logger.LogInformation("Backend jobs turned on");
                services.AddHostedService<ThirtyDaySellServiceHost>();
                services.AddHostedService<PatternMonitoringServiceHost>();
                services.AddHostedService<WeeklyUpsideReversalServiceHost>();
                services.AddHostedService<StopLossServiceHost>();
                services.AddHostedService<BrokerageServiceHost>();
                services.AddHostedService<BrokerageAccountServiceHost>();
                services.AddHostedService<AlertEmailServiceHost>();
            }
            else
            {
                logger.LogInformation("Backend jobs turned off");
            }
        }

        private static void StorageRegistrations(IConfiguration configuration, IServiceCollection services, ILogger logger)
        {
            services.AddSingleton<IOutbox, IncompleteOutbox>();
            
            var storage = configuration.GetValue<string>("storage");

            logger.LogInformation("Read storage configuration type: {storage}", storage);
            
            if (storage == "postgres")
            {
                RegisterPostgresImplemenations(configuration, services);
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

        private static void RegisterPostgresImplemenations(IConfiguration configuration, IServiceCollection services)
        {
            var cnn = configuration.GetValue<string>("DB_CNN");
            services.AddSingleton<IAccountStorage>(s => new storage.postgres.AccountStorage(s.GetRequiredService<IOutbox>(), cnn));
            services.AddSingleton<IBlobStorage>(s => new storage.postgres.PostgresAggregateStorage(s.GetRequiredService<IOutbox>(), cnn));
            services.AddSingleton<IAggregateStorage>(s => new storage.postgres.PostgresAggregateStorage(s.GetRequiredService<IOutbox>(), cnn));
        }
    }
}
