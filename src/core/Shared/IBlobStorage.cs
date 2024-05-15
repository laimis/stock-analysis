#nullable enable

using System.Threading.Tasks;
using Microsoft.FSharp.Core;

namespace core.Shared;


public interface IBlobStorage
{
    Task<FSharpOption<T>> Get<T>(string key);
    Task Save<T>(string key, T t);
}

#nullable restore
