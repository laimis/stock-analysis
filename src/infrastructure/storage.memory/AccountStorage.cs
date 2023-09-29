using core.Account;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;
using Microsoft.FSharp.Core;
using storage.shared;

namespace storage.memory;
public class AccountStorage : MemoryAggregateStorage, IAccountStorage
{
    private static readonly Dictionary<Guid, User?> _users = new();
    private static readonly Dictionary<Guid, ProcessIdToUserAssociation> _associations = new();
    private static readonly Dictionary<Guid, object> _viewModels = new();

    public AccountStorage(IOutbox outbox) : base(outbox)
    {
    }

    public Task Delete(User u)
    {
        _users.Remove(u.Id);
        return Task.CompletedTask;
    }

    public Task<FSharpOption<User>> GetUser(Guid userId)
    {
        var response = _users.TryGetValue(userId, out var u) ? new FSharpOption<User>(u!) : FSharpOption<User>.None;
        
        return Task.FromResult(response);
    }

    public Task<FSharpOption<ProcessIdToUserAssociation>> GetUserAssociation(Guid guid) =>
        Task.FromResult(
            _associations.TryGetValue(guid, out ProcessIdToUserAssociation? association)
                ? new FSharpOption<ProcessIdToUserAssociation>(association)
                : FSharpOption<ProcessIdToUserAssociation>.None
        );

    public Task<FSharpOption<User>> GetUserByEmail(string emailAddress)
    {
        var user = _users.Values.FirstOrDefault(u => u?.State?.Email == emailAddress);
        return Task.FromResult(user == null ? FSharpOption<User>.None : new FSharpOption<User>(user));
    }

    public Task<IEnumerable<EmailIdPair>> GetUserEmailIdPairs() =>
        Task.FromResult(
            _users.Values.Where(u => u != null).Select(u => new EmailIdPair(email: u!.State.Email, id: u.Id.ToString()))
        );

    public async Task Save(User u)
    {
        _users[u.Id] = u;

        await SaveEventsAsync(agg: u, entity: "user", userId: u.Id);
    }

    public Task SaveUserAssociation(ProcessIdToUserAssociation r)
    {
        _associations[r.Id] = r;
        return Task.CompletedTask;
    }

    public Task SaveViewModel<T>(T user, Guid userId)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }
        
        _viewModels[userId] = user;
        return Task.CompletedTask;
    }

    public Task<T?> ViewModel<T>(Guid userId)
    {
        _viewModels.TryGetValue(userId, out object? vm);
        return Task.FromResult((T?)vm);
    }
}