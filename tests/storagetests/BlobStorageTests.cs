using System;
using System.Threading.Tasks;
using core.Shared;
using Xunit;

namespace storagetests;

public abstract class BlobStorageTests
{
    protected abstract IBlobStorage CreateStorage();
    
    [Fact]
    public async Task CanSaveAndRetrieve()
    {
        var storage = CreateStorage();
        var key = Guid.NewGuid().ToString();
        var expected = "test";
        
        await storage.Save(key, expected);
        var actual = await storage.Get<string>(key);
        
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task RepeatedSaves_Work()
    {
        var storage = CreateStorage();
        
        var key = Guid.NewGuid().ToString();
        var expected = "test";
        
        await storage.Save(key, expected);
        var actual = await storage.Get<string>(key);
        
        Assert.Equal(expected, actual);
        
        expected = "test2";
        
        await storage.Save(key, expected);
    }
    
    [Fact]
    public async Task GetNonExisting_ReturnsNone()
    {
        var storage = CreateStorage();
        var key = Guid.NewGuid().ToString();
        
        var actual = await storage.Get<string>(key);
        
        Assert.Null(actual);
    }
}
