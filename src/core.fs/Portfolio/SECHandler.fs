namespace core.fs.Portfolio

open System
open core.fs
open core.fs.Accounts
open core.Shared
open core.fs.Adapters.Storage
open core.fs.Adapters.SEC

type SearchCompanies = {Query:string}
type GetFilingsForTicker = {Ticker:string}
type GetPortfolioFilings = {UserId:UserId}

[<CLIMutable>]
type CompanySearchResult = 
    {
        ticker: string
        cik: string
        companyName: string
    }

[<CLIMutable>]
type FilingDto =
    {
        description: string
        documentUrl: string
        filingDate: DateTimeOffset
        reportDate: DateTimeOffset
        filing: string
        filingUrl: string
    }

[<CLIMutable>]
type CompanyFilingsDto =
    {
        ticker: string
        filings: FilingDto array
    }

[<CLIMutable>]
type PortfolioFilingsDto =
    {
        tickerFilings: CompanyFilingsDto array
    }

type SECHandler(accountStorage: IAccountStorage, portfolioStorage: IPortfolioStorage, secFilings: ISECFilings) =

    let toFilingDto (filing: CompanyFiling) : FilingDto =
        {
            description = filing.Description
            documentUrl = filing.DocumentUrl
            filingDate = filing.FilingDate
            reportDate = filing.ReportDate
            filing = filing.Filing
            filingUrl = filing.FilingUrl
        }
    
    let toCompanyFilingsDto (ticker: Ticker) (filings: CompanyFiling seq) : CompanyFilingsDto =
        {
            ticker = ticker.Value
            filings = filings |> Seq.map toFilingDto |> Seq.toArray
        }
    
    interface IApplicationService
    
    member _.Handle(query:SearchCompanies) : System.Threading.Tasks.Task<Result<CompanySearchResult array, ServiceError>> = task {
        
        let! results = accountStorage.SearchTickerCik query.Query
    
        let mapped = 
            results 
            |> Seq.map (fun mapping -> 
                {
                    ticker = mapping.Ticker
                    cik = mapping.Cik
                    companyName = mapping.Title
                })
            |> Seq.toArray
        return Ok mapped
    }
    
    member _.Handle(query:GetFilingsForTicker) : System.Threading.Tasks.Task<Result<CompanyFilingsDto, ServiceError>> = task {
        let ticker = Ticker query.Ticker

        let! result = secFilings.GetFilings ticker
    
        match result with
        | Error err -> return Error err
        | Ok companyFilings ->
            let dto = toCompanyFilingsDto ticker companyFilings.Filings
            return Ok dto
    }
    
    member _.Handle(query:GetPortfolioFilings) : System.Threading.Tasks.Task<Result<PortfolioFilingsDto, ServiceError>> = task {
        
        let! stockPositions = portfolioStorage.GetStockPositions query.UserId
        let! optionPositions = portfolioStorage.GetOptionPositions query.UserId
        
        let stockTickers = 
            stockPositions 
            |> Seq.filter _.IsOpen 
            |> Seq.map _.Ticker
        
        let optionTickers = 
            optionPositions 
            |> Seq.filter _.IsOpen 
            |> Seq.map _.UnderlyingTicker
        
        let allTickers = 
            Seq.append stockTickers optionTickers
            |> Seq.distinct
            |> Seq.toArray
        
        // Get filings for each ticker from the last 2 days
        let twoDaysAgo = DateTimeOffset.UtcNow.Date.AddDays -2
        
        let getRecentFilingsForTicker (ticker: Ticker) = async {
            try
                let! result = secFilings.GetFilings ticker |> Async.AwaitTask
                
                match result with
                | Error _ -> return None
                | Ok companyFilings ->
                    let recentFilings = 
                        companyFilings.Filings
                        |> Seq.filter (fun f -> f.FilingDate.Date >= twoDaysAgo)
                        |> Seq.toArray
                    
                    if recentFilings.Length > 0 then
                        return Some (toCompanyFilingsDto ticker recentFilings)
                    else
                        return None
            with
            | _ -> return None
        }
        
        let! results = 
            allTickers
            |> Seq.map getRecentFilingsForTicker
            |> Async.Sequential
        
        let tickerFilings = 
            results 
            |> Seq.choose id 
            |> Seq.toArray
        
        return { tickerFilings = tickerFilings } |> Ok
    }
