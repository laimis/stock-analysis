using System.Collections.Generic;
using core.Shared;

namespace core.Adapters.CSV
{
    public interface ICSVParser
    {
         ServiceResponse<IEnumerable<T>> Parse<T>(string content);
    }
}