﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Adapters.Options
{
    public interface IOptionsService
    {
        Task<string[]> GetOptions(string ticker);
        Task<IEnumerable<OptionDetail>> GetOptionDetails(string ticker, string optionDate);
    }
}