using System;
using core.Account;
using core.Adapters.Stocks;
using core.Options;
using core.Portfolio;
using financialmodelingclient;
using iexclient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using storage.shared;

namespace web
{
    public class DIHelper
    {
        internal static void RegisterServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<IOptionsService>(s =>
            {
                return new IEXClient(configuration.GetValue<string>("IEXClientToken"));
            });
            services.AddSingleton<IStocksService, StocksService>();
            services.AddSingleton<IPortfolioStorage, PortfolioStorage>(); 

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