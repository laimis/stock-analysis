using System;
using core;
using core.Account;
using core.Adapters.CSV;
using core.Adapters.Emails;
using core.Adapters.Subscriptions;
using core.Alerts;
using core.Options;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Cryptos;
using core.Shared.Adapters.CSV;
using core.Shared.Adapters.SEC;
using core.Shared.Adapters.SMS;
using csvparser;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            services.AddMediatR(typeof(Sell).Assembly, typeof(DIHelper).Assembly);
            services.AddSingleton<CookieEvents>();
            services.AddSingleton<IPasswordHashProvider, PasswordHashProvider>();
            services.AddSingleton<ICSVWriter, CsvWriterImpl>();
            services.AddSingleton<ISECFilings, EdgarClient>();
            
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
                new storage.memory.AccountStorage(s.GetRequiredService<IMediator>())
            );

            services.AddSingleton<IAggregateStorage>(s =>
                new storage.memory.MemoryAggregateStorage(s.GetRequiredService<IMediator>())
            );

            services.AddSingleton<IBlobStorage>(s =>
                new storage.memory.MemoryAggregateStorage(s.GetRequiredService<IMediator>())
            );
        }

        private static void RegisterPostgresImplemenations(IConfiguration configuration, IServiceCollection services)
        {
            var cnn = configuration.GetValue<string>("DB_CNN");
            services.AddSingleton<IAccountStorage>(s =>
            {
                return new storage.postgres.AccountStorage(s.GetRequiredService<IMediator>(), cnn);
            });
            services.AddSingleton<IBlobStorage>(s =>
            {
                return new storage.postgres.PostgresAggregateStorage(s.GetRequiredService<IMediator>(), cnn);
            });
            services.AddSingleton<IAggregateStorage>(s =>
            {
                return new storage.postgres.PostgresAggregateStorage(s.GetRequiredService<IMediator>(), cnn);
            });
        }
    }
}