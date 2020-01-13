using core.Account;
using core.Options;
using core.Portfolio;
using core.Stocks;
using financialmodelingclient;
using iexclient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using storage.redis;
// using storage.postgres;

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

            RegisterPostgresImplemenations(configuration, services);
            RegisterRedisImplemenations(configuration, services);
        }

        private static void RegisterRedisImplemenations(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<storage.redis.AggregateStorage>(_ =>
            {
                var cnn = configuration.GetValue<string>("REDIS_CNN");
                return new storage.redis.AggregateStorage(cnn);
            });
        }

        private static void RegisterPostgresImplemenations(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<IPortfolioStorage>(s =>
            {
                var cnn = configuration.GetValue<string>("DB_CNN");
                return new storage.postgres.PortfolioStorage(cnn);
            });
            services.AddSingleton<IAccountStorage>(s =>
            {
                var cnn = configuration.GetValue<string>("DB_CNN");
                return new storage.postgres.AccountStorage(cnn);
            });
            services.AddSingleton<storage.postgres.AggregateStorage>(_ =>
            {
                var cnn = configuration.GetValue<string>("DB_CNN");
                return new storage.postgres.AggregateStorage(cnn);
            });
        }
    }
}