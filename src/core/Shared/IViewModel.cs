using System;

namespace core.Shared
{
    public interface IViewModel
    {
        DateTimeOffset Calculated { get; }
    }
}