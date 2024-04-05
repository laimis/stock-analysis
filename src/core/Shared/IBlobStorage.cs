#nullable enable

using System.Threading.Tasks;

namespace core.Shared;


public interface IBlobStorage
{
    Task<T?> Get<T>(string key);
    Task Save<T>(string key, T t);
}

#nullable restore
