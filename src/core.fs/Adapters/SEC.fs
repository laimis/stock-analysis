namespace core.fs.Adapters.SEC

open System.Threading.Tasks
open core.Shared
open core.fs

[<Struct>]
type FilingDetails =
    {
        Form: string
        FilingDate: System.DateTime
        AcceptedDate: System.DateTime
        PeriodOfReport: System.DateTime
    }

[<Struct>]
type CompanyFiling =
    {
        Description: string
        DocumentsUrl: string
        FilingDate: System.DateTime
        Filing: string
        InteractiveDataUrl: string
    }
    
    with
        member this.IsNew = this.FilingDate > System.DateTime.Now.AddDays(-7)

[<Struct>]
type CompanyFilings(ticker:Ticker, filings:seq<CompanyFiling>) =
    member _.Ticker = ticker
    member _.Filings = filings


type ISECFilings =
 abstract GetFilings : ticker:Ticker  -> Task<Result<CompanyFilings,ServiceError>>
