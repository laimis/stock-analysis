using System.Linq;
using System.Threading.Tasks;
using core.Portfolio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private IPortfolioStorage _storage;

        public PortfolioController(IPortfolioStorage storage)
        {
            this._storage = storage;
        }

        [HttpGet]
        public async Task<object> Index()
        {
            var stocks = await _storage.GetStocks(this.User.Identifier());

            var cashedout = stocks.Where(s => s.State.Owned == 0);
            var owned = stocks.Where(s => s.State.Owned > 0);

            var totalSpent = stocks.Sum(s => s.State.Spent);
            var totalEarned = stocks.Sum(s => s.State.Earned);

            var options = await _storage.GetSoldOptions(this.User.Identifier());

            var ownedOptions = options.Where(o => o.State.Amount > 0);
            var closedOptions = options.Where(o => o.State.Amount == 0);
            
            var obj = new
            {
                totalSpent,
                totalEarned,
                totalCashedOutSpend = cashedout.Sum(s => s.State.Spent),
                totalCashedOutEarnings = cashedout.Sum(s => s.State.Earned),
                owned = owned.Select(o => StocksController.ToOwnedView(o)),
                cashedOut = cashedout.Select(o => StocksController.ToOwnedView(o)),
                ownedOptions = ownedOptions.Select(o => OptionsController.ToOptionView(o)),
                closedOptions = closedOptions.Select(o => OptionsController.ToOptionView(o)),
                pendingPremium = ownedOptions.Sum(o => o.State.Premium),
                collateralCash = ownedOptions.Sum(o => o.State.CollateralCash),
                collateralShares = ownedOptions.Sum(o => o.State.CollateralShares),
                optionEarnings = options.Sum(o => o.State.Profit)
            };

            return obj;
        }
    }
}