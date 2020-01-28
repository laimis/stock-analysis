using System.Collections.Generic;

namespace core.Adapters.CSV
{
    public interface ICSVParser
    {
         IEnumerable<T> Parse<T>(string content);
    }
}