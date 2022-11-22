using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Adapters.Options;
using core.Adapters.Stocks;
using core.Shared.Adapters.Stocks;
using core.Stocks.View;
using iexclient;
using Xunit;
using Xunit.Abstractions;

namespace iexclienttests
{
    [Trait("Category", "Integration")]
    public class IEXClientTests : IClassFixture<IEXClientFixture>
    {
        private string[] _options;
        private OptionDetail[] _optionDetails;
        private Price _price;
        private IEXClient _client;
        private ITestOutputHelper _output;
        private List<StockQueryResult> _mostActive;
        private List<SearchResult> _search;
        private StockAdvancedStats _advanced;
        private PriceBar[] _priceHistory;

        public IEXClientTests(IEXClientFixture fixture, Xunit.Abstractions.ITestOutputHelper output)
        {
            _options = fixture.Options;
            _optionDetails = fixture.OptionDetails.ToArray();
            _price = fixture.Price;
            _client = fixture.Client;
            _output = output;
            _mostActive = fixture.MostActive;
            _search = fixture.SearchResults;
            _advanced = fixture.AdvancedStats;
            _priceHistory = fixture.PriceHistory;
        }

        [Fact]
        public void GetOptions_Returned()
        {
            Assert.NotEmpty(_options);
        }

        [Fact]
        public void OptionHasYear()
        {
            Assert.Equal("20210806", _options[0]);
        }

        [Fact]
        public void OptionDetails_CountCorrect()
        {
            Assert.Equal(20, _optionDetails.Length);
        }

        [Fact]
        public void OptionDetails_PutsSellsMatches()
        {
            var calls = _optionDetails.Count(o => o.IsCall);
            var puts = _optionDetails.Count(o => o.IsPut);

            Assert.Equal(calls, puts);
        }

        [Fact]
        public void OptionDetails_VolumeCorrect()
        {
            var specificCall = _optionDetails.Single(o => o.StrikePrice == 2 && o.IsCall);

            Assert.Equal(51, specificCall.Volume);
        }

        [Fact]
        public void OptionDetails_MaxSpread()
        {
            var maxSpread = _optionDetails.Max(o => o.Spread);

            var strike = _optionDetails.Single(o => o.Spread == maxSpread);

            Assert.Equal(2.2, maxSpread, 1);
            Assert.Equal(7.5, strike.StrikePrice);
        }
        
        [Fact]
        public void Price_Set()
        {
            Assert.True(_price.Amount > 0);
        }

        [Fact]
        public void MostActive_Set()
        {
            Assert.Equal(10, _mostActive.Count);
        }

        [Fact]
        public void SearchWorks()
        {
            Assert.NotEmpty(_search);
            Assert.NotNull(_search.SingleOrDefault(r => r.Symbol == "SFIX"));
        }

        [Fact]
        public void AdvancedHasEarnings()
        {
            Assert.NotNull(_advanced.NextEarningsDate);
        }

        [Fact]
        public void PriceHistory()
        {
            Assert.NotEmpty(_priceHistory);
            Assert.True(_priceHistory[0].Close > 0);
            Assert.NotEmpty(_priceHistory[0].DateStr);
        }
    }
}
