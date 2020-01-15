using System.Linq;
using core;
using core.Options;
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
        private TickerPrice _price;
        private IEXClient _client;
        private ITestOutputHelper _output;

        public IEXClientTests(IEXClientFixture fixture, Xunit.Abstractions.ITestOutputHelper output)
        {
            _options = fixture.Options;
            _optionDetails = fixture.OptionDetails.ToArray();
            _price = fixture.Price;
            _client = fixture.Client;
            _output = output;
        }

        [Fact]
        public void GetOptions_Returned()
        {
            Assert.NotEmpty(_options);
        }

        [Fact]
        public void OptionHasYear()
        {
            Assert.Equal("202001", _options[0]);
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

            Assert.Equal(0.7, maxSpread, 1);
            Assert.Equal(7.5, strike.StrikePrice);
        }
        
        [Fact]
        public void Price_Set()
        {
            Assert.True(_price.Amount > 0);
        }
    }
}
