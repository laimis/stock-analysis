using core.Account;
using core.Options;
using core.Portfolio;
using core.Stocks;
using financialmodelingclient;
using iexclient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using storage.postgres;

namespace web
{
    public class DIHelper
    {
        internal static void RegisterServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<IStocksService, StocksService>();
            services.AddSingleton<IPortfolioStorage>(s =>
            {
                var cnn = configuration.GetValue<string>("DB_CNN");
                return new PortfolioStorage(cnn);
            });
            services.AddSingleton<IAccountStorage>(s =>
            {
                var cnn = configuration.GetValue<string>("DB_CNN");
                return new AccountStorage(cnn);
            });
            services.AddSingleton<IOptionsService>(s =>
            {
                return new IEXClient(configuration.GetValue<string>("IEXClientToken"));
            });
        }
    }
}