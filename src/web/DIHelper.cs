using System;
using core.fs.Alerts;
using core.fs.Shared;
using core.fs.Shared.Adapters.Brokerage;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Adapters.Subscriptions;
using core.Shared.Adapters;
using core.Shared.Adapters.Cryptos;
using core.Shared.Adapters.CSV;
using core.Shared.Adapters.Emails;
using core.Shared.Adapters.SEC;
using core.Shared.Adapters.SMS;
using csvparser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using secedgar;
using securityutils;
using storage.shared;
using web.BackgroundServices;
using web.Utils;

namespace web
{
    public class DIHelper
    {
        internal static void RegisterServices(
            IConfiguration configuration,
            IServiceCollection services,
            ILogger logger)
        {
            services.AddSingleton<IRoleService>(s => new RoleService(
                configuration.GetValue<string>("ADMINEmail")
            ));
            services.AddSingleton<core.fs.Shared.Adapters.Logging.ILogger, GenericLogger>();
            services.AddSingleton<coinmarketcap.CoinMarketCapClient>(s =>
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

            // go over all types in core.fs assembly, find any innner classes and register any type that implements IApplicationService
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

            services.AddSingleton<IBrokerage>(s =>
                new tdameritradeclient.TDAmeritradeClient(
                    s.GetService<ILogger<tdameritradeclient.TDAmeritradeClient>>(),
                    configuration.GetValue<string>("TDAMERITRADE_CALLBACK_URL"),
                    configuration.GetValue<string>("TDAMERITRADE_CLIENT_ID")
                ));

            StorageRegistrations(configuration, services, logger);
            
            services.AddHostedService<ThirtyDaySellService>();
            services.AddHostedService<StockAlertService>();
            services.AddHostedService<WeeklyUpsideReversalService>();
            services.AddHostedService<EmailNotificationService>();
            services.AddHostedService<StopLossServiceHost>();
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