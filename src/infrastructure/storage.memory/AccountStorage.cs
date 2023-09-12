using core.Account;
using core.Shared.Adapters.Storage;
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

    public Task<User?> GetUser(Guid userId)
    {
        _users.TryGetValue(userId, out User? u);
        return Task<User?>.FromResult(u);
    }

    public Task<ProcessIdToUserAssociation?> GetUserAssociation(Guid guid) =>
        Task.FromResult(
            _associations.TryGetValue(guid, out ProcessIdToUserAssociation? association)
                ? association
                : null
        );

    public Task<User?> GetUserByEmail(string emailAddress)
    {
        var user = _users.Values.FirstOrDefault(u => u?.State?.Email == emailAddress);
        return Task.FromResult(user);
    }

    public Task<IEnumerable<(string email, string id)>> GetUserEmailIdPairs() =>
        Task.FromResult(
            _users.Values.Where(u => u != null).Select(u => (u!.State.Email, u.Id.ToString()))
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