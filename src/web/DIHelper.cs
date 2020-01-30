using System;
using core;
using core.Account;
using core.Adapters.CSV;
using core.Adapters.Options;
using core.Adapters.Stocks;
using core.Options;
using csvparser;
using financialmodelingclient;
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
            {
                return new IEXClient(configuration.GetValue<string>("IEXClientToken"));
            });
            services.AddSingleton<IOptionsService>(s => s.GetService<IEXClient>());
            services.AddSingleton<IStocksService2>(s => s.GetService<IEXClient>());
            services.AddSingleton<IStocksService, StocksService>();
            services.AddSingleton<IPortfolioStorage, PortfolioStorage>();
            services.AddSingleton<ICSVParser, CSVParser>();
            services.AddMediatR(typeof(Sell).Assembly);
            services.AddSingleton<CookieEvents>();

            services.AddHostedService<TestService>();
            
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
                return new storage.redis.AccountStorage(cnn);
            });
            services.AddSingleton<IAggregateStorage>(_ =>
            {
                return new storage.redis.AggregateStorage(cnn);
            });
            services.AddSingleton<storage.redis.AggregateStorage>(c => 
                (storage.redis.AggregateStorage)c.GetService<IAggregateStorage>());
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
                return new storage.postgres.AccountStorage(cnn);
            });
            services.AddSingleton<IAggregateStorage>(_ =>
            {
                return new storage.postgres.AggregateStorage(cnn);
            });
        }
    }
}