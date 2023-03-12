using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Shared.Adapters.SEC;

public record FilingDetails
{
    public string Form { get; set; }
    public DateTime FilingDate { get; set; }
    public DateTime AcceptedDate { get; set; }
    public DateTime PeriodOfReport { get; set; }
}
public record CompanyFiling
{
    public string Description { get; init; }
    public string DocumentsUrl { get; init; }
    public DateTime FilingDate { get; init; }
    public string Filing { get; init; }
    public string InteractiveDataUrl { get; init; }
    public bool IsNew => FilingDate > DateTime.Now.AddDays(-7);
    // public FilingDetails Details { get; init; }
}

public record struct CompanyFilings(string ticker, List<CompanyFiling> filings);

public interface ISECFilings
{
    Task<ServiceResponse<CompanyFilings>> GetFilings(string ticker);
}