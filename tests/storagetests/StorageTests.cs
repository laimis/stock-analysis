using Xunit;

namespace storage.postgrestests
{
    [Trait("Category", "Integration")]
    public class StorageTests
    {
        protected static string _cnn = "Server=localhost;Database=stocks;User id=stocks;password=stocks";
    }
}