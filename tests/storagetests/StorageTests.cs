using Xunit;

namespace storagetests
{
    [Trait("Category", "Integration")]
    public class StorageTests
    {
        protected static string _cnn = "Server=localhost;Database=stocks;User id=stocks;password=stocks";
    }
}