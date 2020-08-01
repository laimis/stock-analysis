using System.Collections.Generic;

namespace core.Adapters.CSV
{
    public interface ICSVParser
    {
         (IEnumerable<T>, string error) Parse<T>(string content);
    }
}