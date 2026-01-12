namespace core.fs

open System
open System.Text.RegularExpressions
open core.Shared
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.CSV
open core.fs.Adapters.Email
open core.fs.Adapters.Storage
open core.fs.Options
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
        optionsImport:core.fs.Options.OptionsHandler,
        stocksImport:core.fs.Portfolio.StockPositionHandler) =
        
        interface IApplicationService
        
        member _.Handle(cmd:Command) = task {
            
            let sendEmail (user:User) subject body =
                emailService.Send (Recipient(email=user.State.Email, name=user.State.Name)) Sender.NoReply subject body null
            
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
                    Date = Some r.Date
                    NumberOfShares = r.Quantity.Value
                    Price = r.Price.Value
                    StopPrice = None
                    BrokerageOrderId = None
                }
            
            let! user = accounts.GetUser(cmd.UserId)
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->
                
                let records = cmd.Content |> csvParser.Parse<TransactionRecord>
                match records with
                | Error err -> return Error err
                | Ok records ->
                    let! emailResponse = sendEmail user "Started importing transactions" "Email will follow when complete."

                    match emailResponse with
                    | Error err -> return ServiceError err |> Error
                    | Ok () ->
                    
                    let! processed =
                        records
                        |> Seq.rev
                        |> Seq.filter(_.Qualify())
                        |> Seq.map(fun r -> async {
                            match r.IsOption(),r.IsStock(),r.IsBuy(),r.IsSell() with
                            | true, _, true, _ -> // buy option
                                // let data = r |> createOptionTransaction
                                // let cmd = BuyOrSellCommand.Buy(data, cmd.UserId)
                                // let! result = optionsImport.Handle(cmd) |> Async.AwaitTask
                                // TODO: implement
                                return Ok ()
                            | true, _, _, true -> // sell option
                                // let data = r |> createOptionTransaction
                                // let cmd = BuyOrSellCommand.Sell(data, cmd.UserId)
                                // let! result = optionsImport.Handle(cmd) |> Async.AwaitTask
                                // return result |> Result.bind (fun _ -> Ok ())
                                // TODO: implement
                                return Ok()
                            | _, true, true, _ -> // buy stock
                                let data = r |> createStockTransaction 
                                let cmd = core.fs.Portfolio.Buy(data, cmd.UserId)
                                return! stocksImport.Handle(cmd) |> Async.AwaitTask
                            | _, true, _, true -> // sell stock
                                let data = r |> createStockTransaction
                                let cmd = core.fs.Portfolio.Sell(data, cmd.UserId)
                                return! stocksImport.Handle(cmd) |> Async.AwaitTask
                            | _ -> return Ok ()
                        })
                        |> Async.Sequential
                        |> Async.StartAsTask
                    
                    let okOrError = processed |> ResponseUtils.toOkOrConcatErrors
                    
                    match okOrError with
                    | Ok _ -> 
                        let! emailResult = sendEmail user "Finished importing transactions" "Finished importing your transactions."
                        match emailResult with
                        | Error err -> return ServiceError err |> Error
                        | Ok () -> return Ok ()
                    | Error err ->
                        return $"Failed to import transactions: {err.Message}" |> ServiceError |> Error
        }
