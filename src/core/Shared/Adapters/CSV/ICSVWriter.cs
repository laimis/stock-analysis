using System.Collections.Generic;

namespace core.Shared.Adapters.CSV
{
    public interface ICSVWriter
    {
        string Generate<T>(IEnumerable<T> rows);   
    }
}