using core.Options;
using core.Utils;
using Xunit;

namespace coretests.Utils
{
    public class ValidValuesTests
    {
        private ValidValues _att;

        public ValidValuesTests()
        {
            _att = new ValidValues("CALL", "PUT");
        }

        [Fact]
        public void Success()
        {
            Assert.True(_att.IsValid(OptionType.CALL.ToString()));
        }

        [Fact]
        public void Failure()
        {
            Assert.False(_att.IsValid("somethingelse"));
        }

        [Fact]
        public void Message_Formatted()
        {
            var formatted = _att.FormatErrorMessage("myparam");

            Assert.Contains("myparam", formatted);
            Assert.Contains("CALL", formatted);
            Assert.Contains("PUT", formatted);
        }
    }
}