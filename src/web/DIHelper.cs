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
using csvparser;
using iexclient;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using storage.redis;
using storage.shared;
using web.Utils;

namespace web
{
    public class DIHelper
    {
        internal static void RegisterServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<IEXClient>(s =>
                new IEXClient(
                    configuration.GetValue<string>("IEXClientToken")
                )
            );
            
            services.AddSingleton<IOptionsService>(s => s.GetService<IEXClient>());
            services.AddSingleton<IStocksService2>(s => s.GetService<IEXClient>());
            services.AddSingleton<IPortfolioStorage, PortfolioStorage>();
            services.AddSingleton<IAlertsStorage, AlertsStorage>();
            services.AddSingleton<ICSVParser, CSVParser>();
            services.AddSingleton<MarketHours>();
            services.AddSingleton<StockMonitorContainer>();
            services.AddMediatR(typeof(Sell).Assembly);
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
            
            StorageRegistrations(configuration, services);
        }

        private static void StorageRegistrations(IConfiguration configuration, IServiceCollection services)
        {
            var storage = configuration.GetValue<string>("storage");

            if (storage == "postgres")
            {
                RegisterPostgresImplemenations(configuration, services);
            }
            else if (storage == "redis")
            {
                RegisterRedisImplemenations(configuration, services);
            }
            else
            {
                throw new InvalidOperationException(
                    $"configuration 'storage' has value '{storage}', only 'redis' or 'postgres' is supported"
                );
            }
        }

        private static void RegisterRedisImplemenations(IConfiguration configuration, IServiceCollection services)
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