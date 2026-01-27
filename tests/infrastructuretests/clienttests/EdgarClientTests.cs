using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Adapters.SEC;
using core.fs.Adapters.Storage;
using core.fs.Accounts;
using core.fs.Adapters.Brokerage;
using core.fs.Options;
using core.fs.Alerts;
using core.Shared;
using Microsoft.FSharp.Core;
using Xunit;

namespace secedgartests;

/// <summary>
/// Mock IAccountStorage that returns hardcoded CIK mappings for test tickers
/// </summary>
public class FakeAccountStorageForEdgarTests : IAccountStorage
{
    private static readonly Dictionary<string, TickerCikMapping> _tickerCikMappings = new()
    {
        { "AAPL", new TickerCikMapping { Ticker = "AAPL", Cik = "0000320193", Title = "Apple Inc.", LastUpdated = DateTimeOffset.UtcNow } },
        { "MSFT", new TickerCikMapping { Ticker = "MSFT", Cik = "0000789019", Title = "Microsoft Corp", LastUpdated = DateTimeOffset.UtcNow } },
        { "DOCN", new TickerCikMapping { Ticker = "DOCN", Cik = "0001582961", Title = "DigitalOcean Holdings Inc.", LastUpdated = DateTimeOffset.UtcNow } }
    };

    public Task<FSharpOption<TickerCikMapping>> GetTickerCik(string ticker)
    {
        if (_tickerCikMappings.TryGetValue(ticker.ToUpper(), out var mapping))
        {
            return Task.FromResult(FSharpOption<TickerCikMapping>.Some(mapping));
        }
        return Task.FromResult(FSharpOption<TickerCikMapping>.None);
    }

    // Not implemented - not needed for these tests
    public Task<FSharpOption<User>> GetUserByEmail(string emailAddress) => throw new NotImplementedException();
    public Task<FSharpOption<User>> GetUser(UserId userId) => throw new NotImplementedException();
    public Task Save(User u) => throw new NotImplementedException();
    public Task Delete(User u) => throw new NotImplementedException();
    public Task SaveUserAssociation(ProcessIdToUserAssociation r) => throw new NotImplementedException();
    public Task<IEnumerable<AccountBalancesSnapshot>> GetAccountBalancesSnapshots(DateTimeOffset start, DateTimeOffset end, UserId userId) => throw new NotImplementedException();
    public Task SaveAccountBalancesSnapshot(UserId userId, AccountBalancesSnapshot balances) => throw new NotImplementedException();
    public Task<IEnumerable<StockOrder>> GetAccountBrokerageOrders(UserId userId) => throw new NotImplementedException();
    public Task SaveAccountBrokerageStockOrders(UserId userId, IEnumerable<StockOrder> orders) => throw new NotImplementedException();
    public Task SaveAccountBrokerageOptionOrders(UserId userId, IEnumerable<OptionOrder> orders) => throw new NotImplementedException();
    public Task InsertAccountBrokerageTransactions(UserId userId, IEnumerable<AccountTransaction> transactions) => throw new NotImplementedException();
    public Task SaveAccountBrokerageTransactions(UserId userId, AccountTransaction[] transactions) => throw new NotImplementedException();
    public Task<IEnumerable<AccountTransaction>> GetAccountBrokerageTransactions(UserId userId) => throw new NotImplementedException();
    public Task<FSharpOption<ProcessIdToUserAssociation>> GetUserAssociation(Guid guid) => throw new NotImplementedException();
    public Task<IEnumerable<EmailIdPair>> GetUserEmailIdPairs() => throw new NotImplementedException();
    public Task<IEnumerable<OptionPricing>> GetOptionPricing(UserId userId, OptionTicker symbol) => throw new NotImplementedException();
    public Task SaveOptionPricing(OptionPricing pricing, UserId userId) => throw new NotImplementedException();
    public Task<IEnumerable<StockPriceAlert>> GetStockPriceAlerts(UserId userId) => throw new NotImplementedException();
    public Task SaveStockPriceAlert(StockPriceAlert alert) => throw new NotImplementedException();
    public Task DeleteStockPriceAlert(Guid alertId) => throw new NotImplementedException();
    public Task<IEnumerable<Reminder>> GetReminders(UserId userId) => throw new NotImplementedException();
    public Task SaveReminder(Reminder reminder) => throw new NotImplementedException();
    public Task DeleteReminder(Guid reminderId) => throw new NotImplementedException();
    public Task SaveTickerCikMappings(IEnumerable<TickerCikMapping> mappings) => throw new NotImplementedException();
    public Task<IEnumerable<TickerCikMapping>> GetAllTickerCikMappings() => throw new NotImplementedException();
    public Task<FSharpOption<DateTimeOffset>> GetTickerCikLastUpdated() => throw new NotImplementedException();
    public Task<IEnumerable<TickerCikMapping>> SearchTickerCik(string query) => throw new NotImplementedException();
}

[Trait("Category", "Integration")]
public class EdgarClientTests
{
    [Theory]
    [InlineData("AAPL")]
    [InlineData("MSFT")]
    [InlineData("DOCN")]
    public async Task TestAsync(string ticker)
    {
        var mockStorage = new FakeAccountStorageForEdgarTests();
        var client = new secedgar.fs.EdgarClient(
            FSharpOption<Microsoft.Extensions.Logging.ILogger<secedgar.fs.EdgarClient>>.None,
            FSharpOption<IAccountStorage>.Some(mockStorage)
        ) as ISECFilings;

        var response = await client.GetFilings(new Ticker(ticker));

        // make sure it is not an error
        Assert.True(response.IsOk);
        Assert.NotEmpty(response.ResultValue.Filings);
    }
}
