using System.Threading.Tasks;

namespace storage.shared
{
    public interface IBlobStorage
    {
        Task<T> Get<T>(string key);
        Task Save<T>(string key, T t);
    }
}