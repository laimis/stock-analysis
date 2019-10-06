using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Options;
using Microsoft.AspNetCore.Mvc;
using web.Models;

namespace web.Controllers
{
    [Route("api/[controller]")]
    public class OptionsController : Controller
    {
        private IOptionsService _options;

        public OptionsController(IOptionsService options)
        {
            _options = options;
        }

        [HttpGet("{ticker}")]
        public async Task<OptionDetailsViewModel> Details(string ticker)
        {
            var dates = await _options.GetOptions(ticker);
            
            var upToFour = dates.Take(4);

            var options = new List<OptionDetail>();

            foreach(var d in upToFour)
            {
                var details = await _options.GetOptionDetails(ticker, d);
                options.AddRange(details);
            }

            var price = await _options.GetPrice(ticker);

            return OptionDetailsViewModelMapper.Map(price, options);
        }
    }
}