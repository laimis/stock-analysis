namespace core.fs

open System
open System.Text.RegularExpressions
open core.Shared
open core.fs.Options
open core.fs.Shared
open core.fs.Shared.Adapters.CSV
open core.fs.Shared.Adapters.Email
open core.fs.Shared.Adapters.Storage
open core.fs.Shared.Domain
open core.fs.Shared.Domain.Accounts
open core.fs.Stocks

module ImportTransactions =
    
    type Command =
        {
            Content:string
            UserId:UserId
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
            OptionType.FromString m.Groups[4].Value
            
        member this.GetTickerFromOptionDescription() =
            let m = TransactionRecord.OptionRegex.Match(this.Description)
            m.Groups[1].Value
            
    type Handler(
        accounts:IAccountStorage,
        emailService:IEmailService,
        csvParser:ICSVParser,
        optionsImport:core.fs.Options.Handler,
        stocksImport:core.fs.Portfolio.Handler) =
        
        interface IApplicationService
        
        member _.Handle(cmd:Command) = task {
            
            let sendEmail (user:User) subject body =
                emailService.Send (Recipient(email=user.State.Email, name=user.State.Name)) Sender.NoReply subject body
            
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
                
            let createStockTransaction r : core.fs.Portfolio.StockTransaction =
                {
                    PositionId = StockPositionId.create()
                    Date = Nullable<DateTimeOffset> r.Date
                    Notes = Some r.Description
                    NumberOfShares = r.Quantity.Value
                    Price = r.Price.Value
                    StopPrice = Nullable<decimal>()
                    BrokerageOrderId = None
                }
            
            let! user = accounts.GetUser(cmd.UserId)
            match user with
            | None -> return "User not found" |> ResponseUtils.failed
            | Some user ->
                
                let records = cmd.Content |> csvParser.Parse<TransactionRecord>
                match records.Success with
                | None -> return records.Error.Value.Message |> ResponseUtils.failed
                | Some records ->
                    do! sendEmail user "Started importing transactions" ""
                    
                    
                    let! processed =
                        records
                        |> Seq.rev
                        |> Seq.filter(fun r -> r.Qualify())
                        |> Seq.map(fun r -> async {
                            match r.IsOption(),r.IsStock(),r.IsBuy(),r.IsSell() with
                            | true, _, true, _ -> // buy option
                                let data = r |> createOptionTransaction
                                let cmd = BuyOrSellCommand.Buy(data, cmd.UserId)
                                let! result = optionsImport.Handle(cmd) |> Async.AwaitTask
                                return result |> ResponseUtils.toOkOrError
                            | true, _, _, true -> // sell option
                                let data = r |> createOptionTransaction
                                let cmd = BuyOrSellCommand.Sell(data, cmd.UserId)
                                let! result = optionsImport.Handle(cmd) |> Async.AwaitTask
                                return result |> ResponseUtils.toOkOrError
                            | _, true, true, _ -> // buy stock
                                let data = r |> createStockTransaction 
                                let cmd = core.fs.Portfolio.Buy(data, cmd.UserId)
                                let! result = stocksImport.Handle(cmd) |> Async.AwaitTask
                                return result
                            | _, true, _, true -> // sell stock
                                let data = r |> createStockTransaction
                                let cmd = core.fs.Portfolio.Sell(data, cmd.UserId)
                                let! result = stocksImport.Handle(cmd) |> Async.AwaitTask
                                return result
                            | _ -> return Ok
                        })
                        |> Async.Sequential
                        |> Async.StartAsTask
                    
                    let okOrError = processed |> ResponseUtils.toOkOrConcatErrors
                    
                    match okOrError with
                    | Ok -> 
                        do! sendEmail user "Finished importing transactions" ""
                        return Ok
                    | Error err ->
                        return $"Failed to import transactions: {err.Message}" |> ResponseUtils.failed
        }