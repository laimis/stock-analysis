namespace core.fs

open System
open System.Text.RegularExpressions
open core.Account
open core.Shared
open core.Shared.Adapters.CSV
open core.Shared.Adapters.Emails
open core.fs.Options
open core.fs.Shared
open core.fs.Shared.Adapters.Storage
open core.fs.Stocks

module ImportTransactions =
    
    type Command =
        {
            Content:string
            UserId:Guid
        }
        
    type internal TransactionRecord =
        {
            Date:DateTimeOffset
            TransactionId:string
            Description:string
            Quantity:Nullable<decimal>
            Symbol:string
            Price:Nullable<decimal>
        }
        
        static member OptionRegex = Regex(@"(\w+) (\w+ \d+ \d+) (\d{1,6}.\d{1,4}) (Put|Call)")
        
        member this.IsBuy() =
            this.Description.StartsWith("Bought ")
            
        member this.IsSell() =
            this.Description.StartsWith("Sold ")
            
        member this.IsOption() =
            TransactionRecord.OptionRegex.IsMatch(this.Description)
        
        member this.IsStock() =
            this.IsOption() |> not
        
        member this.Qualify() =
            this.IsBuy() || this.IsSell()
            
        member this.StrikePrice() =
            let m = TransactionRecord.OptionRegex.Match(this.Description)
            Decimal.Parse(m.Groups[3].Value)
            
        member this.ExpirationDate() =
            let m = TransactionRecord.OptionRegex.Match(this.Description)
            DateTimeOffset.ParseExact(m.Groups[2].Value, "MMM dd yyyy", null)
            
        member this.OptionType() =
            let m = TransactionRecord.OptionRegex.Match(this.Description)
            m.Groups[4].Value
            
        member this.GetTickerFromOptionDescription() =
            let m = TransactionRecord.OptionRegex.Match(this.Description)
            m.Groups[1].Value
            
    type Handler(
        accounts:IAccountStorage,
        emailService:IEmailService,
        csvParser:ICSVParser,
        optionsImport:core.fs.Options.Handler,
        stocksImport:core.fs.Stocks.Handler) =
        
        interface IApplicationService
        
        member _.Handle(cmd:Command) = task {
            
            let sendEmail (user:User) subject body = task {
                do! emailService.Send(
                    recipient=Recipient(email=user.State.Email, name=user.State.Name),
                    sender=Sender.NoReply,
                    subject=subject,
                    body=body
                )
            }
            
            let createOptionTransaction r =
                {
                    Filled = Nullable<DateTimeOffset> r.Date
                    Notes = r.Description
                    NumberOfContracts = int r.Quantity.Value
                    Premium = Nullable<decimal> (r.Price.Value * 100m)
                    StrikePrice = Nullable<decimal>(r.StrikePrice())
                    ExpirationDate = Nullable<DateTimeOffset>(r.ExpirationDate())
                    OptionType = r.OptionType()
                    Ticker = Ticker(r.GetTickerFromOptionDescription())
                }
                
            let createStockTransaction r =
                {
                    Ticker = Ticker(r.Symbol)
                    Date = Nullable<DateTimeOffset> r.Date
                    Notes = r.Description
                    NumberOfShares = r.Quantity.Value
                    Price = r.Price.Value
                    StopPrice = Nullable<decimal>()
                    BrokerageOrderId = null
                    Strategy = null 
                }
            
            let! user = accounts.GetUser(cmd.UserId)
            match user with
            | None -> return "User not found" |> ResponseUtils.failed
            | Some user ->
                
                let records = cmd.Content |> csvParser.Parse<TransactionRecord>
                match records.IsOk with
                | false -> return records.Error.Message |> ResponseUtils.failed
                | true ->
                    do! sendEmail user "Started importing transactions" ""
                    
                    
                    let! processed =
                        records.Success
                        |> Seq.rev
                        |> Seq.filter(fun r -> r.Qualify())
                        |> Seq.map(fun r -> async {
                            match r.IsOption(),r.IsStock(),r.IsBuy(),r.IsSell() with
                            | true, _, true, _ -> // buy option
                                let data = r |> createOptionTransaction
                                let cmd = BuyOrSellCommand.Buy(data, cmd.UserId)
                                let! result = optionsImport.Handle(cmd) |> Async.AwaitTask
                                match result.IsOk with
                                | false -> return result.Error.Message
                                | true -> return ""
                            | true, _, _, true -> // sell option
                                let data = r |> createOptionTransaction
                                let cmd = BuyOrSellCommand.Sell(data, cmd.UserId)
                                let! result = optionsImport.Handle(cmd) |> Async.AwaitTask
                                match result.IsOk with
                                | false -> return result.Error.Message
                                | true -> return ""
                            | _, true, true, _ -> // buy stock
                                let data = r |> createStockTransaction 
                                let cmd = Buy(data, cmd.UserId)
                                let! result = stocksImport.Handle(cmd) |> Async.AwaitTask
                                match result.IsOk with
                                | false -> return result.Error.Message
                                | true -> return ""
                            | _, true, _, true -> // sell stock
                                let data = r |> createStockTransaction
                                let cmd = Sell(data, cmd.UserId)
                                let! result = stocksImport.Handle(cmd) |> Async.AwaitTask
                                match result.IsOk with
                                | false -> return result.Error.Message
                                | true -> return ""
                            | _ -> return ""
                        })
                        |> Async.Sequential
                        |> Async.StartAsTask
                        
                    let failed = processed |> Seq.filter(fun r -> r <> "")
                    
                    match failed |> Seq.isEmpty with
                    | true -> 
                        do! sendEmail user "Finished importing transactions" ""
                        return ServiceResponse()
                    | false ->
                        return "Failed to import the following transactions: " + String.Join(", ", failed) |> ResponseUtils.failed
        }