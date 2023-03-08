using System;

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
    // public FilingDetails Details { get; init; }
}