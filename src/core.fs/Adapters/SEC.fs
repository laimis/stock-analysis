namespace core.fs.Adapters.SEC

open System.Threading.Tasks
open core.Shared
open core.fs

[<Struct>]
type FilingDetails =
    {
        Form: string
        FilingDate: System.DateTimeOffset
        AcceptedDate: System.DateTimeOffset
        PeriodOfReport: System.DateTimeOffset
    }

module FilingDate =
    /// Returns true if the given "yyyy-MM-dd" filing date string is today or yesterday
    /// relative to the provided reference date.
    let isRecent (referenceDate: System.DateOnly) (filingDate: string) =
        let fmt = "yyyy-MM-dd"
        filingDate = referenceDate.ToString fmt
        || filingDate = referenceDate.AddDays(-1).ToString fmt

[<Struct>]
[<CLIMutable>]
type CompanyFiling =
    {
        Description: string
        DocumentUrl: string
        FilingDate: string
        ReportDate: string option
        Filing: string
        FilingUrl: string
        IsXBRL: bool
        IsInlineXBRL: bool
    }

    member this.IsRecentFor (referenceDate: System.DateOnly) =
        FilingDate.isRecent referenceDate this.FilingDate

[<Struct>]
type CompanyFilings(ticker:Ticker, filings:CompanyFiling array) =
    member _.Ticker = ticker
    member _.Filings = filings


[<Struct>]
[<CLIMutable>]
type CompanyTickerEntry = {
    cik_str: string
    ticker: string
    title: string
}

type ISECFilings =
 abstract GetFilings : ticker:Ticker  -> Task<Result<CompanyFilings,ServiceError>>
 abstract FetchCompanyTickers : unit -> Task<System.Collections.Generic.Dictionary<string, CompanyTickerEntry>>
 abstract FetchPrimaryDocument : filingUrl:string -> Task<Result<string, ServiceError>>

module FilingDescriptions =
    
    /// Returns a user-friendly description for common SEC filing types
    let getFriendlyDescription (filingType: string) : string option =
        match filingType.Trim().ToUpperInvariant() with
        | "3" -> Some "Initial insider ownership filing"
        | "4" -> Some "Insider buying, selling, or awards"
        | "5" -> Some "Insider annual transaction summary"
        | "8-K" -> Some "Material corporate update"
        | "10-Q" -> Some "Quarterly financial report"
        | "10-K" -> Some "Annual financial report"
        | "144" -> Some "Planned insider sale"
        | "ARS" -> Some "Shareholder annual report"
        | "S-1" -> Some "Initial public offering registration"
        | "S-8" -> Some "Employee equity compensation registration"
        | "SCHEDULE 13D" | "SC 13D" -> Some "Active or activist ownership disclosure"
        | "SCHEDULE 13D/A" | "SC 13D/A" -> Some "Active or activist ownership disclosure - Amendment"
        | "SCHEDULE 13G" | "SC 13G" -> Some "Passive large-shareholder disclosure"
        | "SCHEDULE 13G/A" | "SC 13G/A" -> Some "Passive large-shareholder disclosure - Amendment"
        | "SCHEDULE 13F" | "13F-HR" | "13F-HR/A" -> Some "Quarterly holdings of large investment managers"
        | "DEF 14A" -> Some "Annual meeting and voting details"
        | "DEFA14A" -> Some "Supplemental shareholder voting information"
        | "PRE 14A" -> Some "Draft proxy statement (not final)"
        | _ -> None
    
    /// Gets the best description: uses existing if meaningful, otherwise returns friendly fallback
    let getBestDescription (existingDescription: string) (filingType: string) : string =
        match getFriendlyDescription filingType with
        | Some friendly -> friendly
        | None -> existingDescription // Keep original if no friendly version available
