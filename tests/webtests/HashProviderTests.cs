using web.Utils;
using Xunit;

namespace webtests
{
    public class HashProviderTests
    {
        [Fact]
        public void HashesWork()
        {
            var provider = new PasswordHashProvider();

            var generated = provider.Generate("testpassword", 32);

            var generatedAgain = provider.Generate("testpassword", generated.Salt);

            Assert.Equal(generated.Hash, generatedAgain);
        }
    }
}