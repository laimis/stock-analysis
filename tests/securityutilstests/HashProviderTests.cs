using securityutils;
using Xunit;

namespace securityutilstests
{
    public class HashProviderTests
    {
        [Fact]
        public void HashesWork()
        {
            var provider = new PasswordHashProvider();

            var generated = provider.GenerateHashAndSalt("testpassword", 32);

            var generatedAgain = provider.GenerateHash("testpassword", generated.Salt);

            Assert.Equal(generated.Hash, generatedAgain);
        }
    }
}