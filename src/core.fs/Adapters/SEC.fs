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

[<Struct>]
[<CLIMutable>]
type CompanyFiling =
    {
        Description: string
        DocumentUrl: string
        FilingDate: System.DateTimeOffset
        ReportDate: System.DateTimeOffset
        Filing: string
        FilingUrl: string
    }
    
    with
        member this.IsNew = this.FilingDate > System.DateTimeOffset.Now.AddDays -7

[<Struct>]
type CompanyFilings(ticker:Ticker, filings:seq<CompanyFiling>) =
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
