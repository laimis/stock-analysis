using Xunit;

using storage.tests
{
    [Trait("Category", "Integration")]
    public class StorageTests
    {
        protected static string _cnn = "Server=localhost;Database=stocks;User id=stocks;password=stocks";
    }
}