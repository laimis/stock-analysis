using System.Collections.Generic;

namespace core.Shared.Adapters.CSV
{
    public interface ICSVParser
    {
         ServiceResponse<IEnumerable<T>> Parse<T>(string content);
    }
}