using System;
using core;
using core.Account;
using core.Adapters.CSV;
using core.Adapters.Emails;
using core.Adapters.Options;
using core.Adapters.Stocks;
using core.Adapters.Subscriptions;
using core.Alerts;
using core.Options;
using core.Shared.Adapters.Cryptos;
using csvparser;
using iexclient;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using storage.redis;
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
            services.AddSingleton<IEXClient>(s =>
                new IEXClient(
                    configuration.GetValue<string>("IEXClientToken"),
                    s.GetService<ILogger<IEXClient>>()
                )
            );
            
            services.AddSingleton<coinmarketcap.CoinMarketCapClient>(s =>
                new coinmarketcap.CoinMarketCapClient(
                    configuration.GetValue<string>("COINMARKETCAPToken")
                )
            );

            services.AddSingleton<IOptionsService>(s => s.GetService<IEXClient>());
            services.AddSingleton<IStocksService2>(s => s.GetService<IEXClient>());
            services.AddSingleton<ICryptoService>(s => s.GetService<coinmarketcap.CoinMarketCapClient>());
            services.AddSingleton<IPortfolioStorage, PortfolioStorage>();
            services.AddSingleton<IAlertsStorage, AlertsStorage>();
            services.AddSingleton<ICSVParser, CSVParser>();
            services.AddSingleton<MarketHours>();
            services.AddSingleton<StockMonitorContainer>();
            services.AddMediatR(typeof(Sell).Assembly, typeof(DIHelper).Assembly);
            services.AddSingleton<CookieEvents>();
            services.AddSingleton<IPasswordHashProvider, PasswordHashProvider>();
            
            services.AddSingleton<ISubscriptions>(s => 
                new stripe.Subscriptions(
                    configuration.GetValue<string>("STRIPE_API_KEY")
                )
            );

            services.AddSingleton<IEmailService>(s => 
                new sendgridclient.SendGridClientImpl(
                    configuration.GetValue<string>("SENDGRID_API_KEY")
                )
            );

            services.AddHostedService<StockMonitorService>();
            services.AddHostedService<ThirtyDaySellService>();
            services.AddHostedService<UserChangedScheduler>();
            
            StorageRegistrations(configuration, services, logger);
        }

        private static void StorageRegistrations(IConfiguration configuration, IServiceCollection services, ILogger logger)
        {
            var storage = configuration.GetValue<string>("storage");

            if (storage == "postgres")
            {
                RegisterPostgresImplemenations(configuration, services);
            }
            else if (storage == "redis")
            {
                RegisterRedisImplemenations(configuration, services, logger);
            }
            else if (storage == "memory")
            {
                RegisterMemoryImplementations(configuration, services);
            }
            else
            {
                throw new InvalidOperationException(
                    $"configuration 'storage' has value '{storage}', only 'redis', 'postgres', or 'memory' is supported"
                );
            }
        }

        private static void RegisterMemoryImplementations(IConfiguration configuration, IServiceCollection services)
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

        private static void RegisterRedisImplemenations(IConfiguration configuration, IServiceCollection services, ILogger logger)
        {
            var cnn = configuration.GetValue<string>("REDIS_CNN");
            
            services.AddSingleton<IAccountStorage>(s =>
            {
                return new storage.redis.AccountStorage(s.GetRequiredService<IMediator>(), cnn);
            });
            services.AddSingleton<IAggregateStorage>(s =>
            {
                return new storage.redis.RedisAggregateStorage(s.GetRequiredService<IMediator>(), cnn);
            });
            services.AddSingleton<IBlobStorage>(s => 
            {
                return new storage.redis.RedisAggregateStorage(s.GetRequiredService<IMediator>(), cnn);
            });
            services.AddSingleton<storage.redis.RedisAggregateStorage>(c => 
                (storage.redis.RedisAggregateStorage)c.GetService<IAggregateStorage>());
            services.AddSingleton<Migration>(_ =>
            {
                return new storage.redis.Migration(cnn);
            });
        }

        private static void RegisterPostgresImplemenations(IConfiguration configuration, IServiceCollection services)
        {
            var cnn = configuration.GetValue<string>("DB_CNN");
            services.AddSingleton<IAccountStorage>(s =>
            {
                return new storage.postgres.AccountStorage(s.GetRequiredService<IMediator>(), cnn);
            });
            services.AddSingleton<IAggregateStorage>(s =>
            {
                return new storage.postgres.PostgresAggregateStorage(s.GetRequiredService<IMediator>(), cnn);
            });
        }
    }
}