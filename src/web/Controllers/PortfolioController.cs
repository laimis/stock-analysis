using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using core.Portfolio;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
	[Route("api/[controller]")]
	public class PortfolioController : Controller
	{
		private IPortfolioStorage _storage;
		const string _user = "laimonas";

		public PortfolioController(IPortfolioStorage storage)
		{
			this._storage = storage;
		}

		[HttpGet]
		public async Task<object> Stocks()
		{
			var stocks = await _storage.GetStocks(_user);

			var cashedout = stocks.Where(s => s.State.Owned == 0);
			var owned = stocks.Where(s => s.State.Owned > 0);

			var totalSpent = stocks.Sum(s => s.State.Spent);
			var totalEarned = stocks.Sum(s => s.State.Earned);

			return new {
				totalSpent,
				totalEarned,
				totalCashedOutSpend = cashedout.Sum(s => s.State.Spent),
				totalCashedOutEarnings = cashedout.Sum(s => s.State.Earned),
				owned = owned.Select(o => ToOwnedView(o)),
				cashedOut = cashedout.Select(o => ToOwnedView(o))
			};
		}

		[HttpGet("csv")]
		public async Task<ActionResult> CSV()
		{
			var stocks = await _storage.GetStocks(_user);

			var builder = new StringBuilder();

			foreach(var s in stocks)
			{
				builder.AppendLine($"{s.State.Ticker},{s.State.Spent},{s.State.Earned}");
			}

			return new ContentResult {
				Content = builder.ToString(),
				ContentType = "text/csv"
			};
		}

		private object ToOwnedView(OwnedStock o)
		{
			return new {
				ticker = o.State.Ticker,
				owned = o.State.Owned,
				spent = Math.Round(o.State.Spent, 2),
				earned = Math.Round(o.State.Earned, 2)
			};
		}

		[HttpPost("purchase")]
		public async Task<ActionResult> PurchaseAsync([FromBody]PurchaseModel model)
		{
			var stock = await this._storage.GetStock(model.Ticker, _user);

			if (stock == null)
			{
				stock = new OwnedStock(model.Ticker, _user);
			}

			stock.Purchase(model.Amount, model.Price, model.Date);

			await this._storage.Save(stock);

			return Ok();
		}

		[HttpPost("sell")]
		public async Task<ActionResult> SellAsync([FromBody]PurchaseModel model)
		{
			var stock = await this._storage.GetStock(model.Ticker, _user);

			if (stock == null)
			{
				return NotFound();
			}

			stock.Sell(model.Amount, model.Price, model.Date);

			await this._storage.Save(stock);

			return Ok();
		}
	}
}