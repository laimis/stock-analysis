namespace core.fs.Cryptos

    open System
    open System.ComponentModel.DataAnnotations
    open core.Cryptos
    open core.Shared
    open core.fs
    open core.fs.Accounts
    open core.fs.Adapters.CSV
    open core.fs.Adapters.Cryptos
    open core.fs.Adapters.Storage
    open core.fs.Cryptos.Import
    open core.fs.Services
    open core.fs.Adapters.Storage
    
    type CryptoTransaction =
        {
            [<Range(0.00000000000000000001, 1000000.0)>]
            Quantity:decimal
            [<Range(0, 100000)>]
            DollarAmount:decimal
            [<Required>]
            Date:DateTimeOffset
            [<Required>]
            Token:Token
            Notes:string
        }
        
    type CryptoTransactionWithUserId =
        | Buy of CryptoTransaction * UserId
        | Sell of CryptoTransaction * UserId
        | Reward of CryptoTransaction * UserId
        | Yield of CryptoTransaction * UserId
        
    type DashboardQuery =
        {
            UserId:UserId
        }
        
    type DeleteTransaction =
        {
            UserId:UserId
            TransactionId:Guid
            Token:Token
        }
        
    type Details =
        {
            Token:Token
        }
        
    type Export =
        {
            UserId:UserId
        }
        
    type ImportBlockFi =
        {
            UserId:UserId
            Content:string
        }
        
    type ImportCoinbase =
        {
            UserId:UserId
            Content:string
        }
        
    type ImportCoinbasePro =
        {
            UserId:UserId
            Content:string
        }
        
    type ImportCryptoCommand =
        | ImportBlockFi of ImportBlockFi
        | ImportCoinbase of ImportCoinbase
        | ImportCoinbasePro of ImportCoinbasePro
        
    type OwnershipQuery =
        {
            Token:Token
            UserId:UserId
        }
    
    type OwnedCryptoView(owned:OwnedCryptoState) =
        member _.Token = owned.Token
        member _.Quantity = owned.Quantity
        member _.Cost = owned.Cost
        member _.DaysHeld = owned.DaysHeld
        member _.AverageCost = owned.AverageCost
        
    type CryptoDashboardView(owned:OwnedCryptoView list) =
        member _.Owned = owned
        
    type CryptoDetailsView(token:Token, price:Nullable<Price>) =
        member _.Token = token
        member _.Price = price
                
    type CryptoOwnershipView(state:OwnedCryptoState) =
        member _.Id = state.Id
        member _.Token = state.Token
        member _.Quantity = state.Quantity
        member _.Cost = state.Cost
        member _.AverageCost = state.AverageCost
        member _.Transactions =
            state.Transactions
            |> Seq.map (fun t -> t.ToSharedTransaction())
            |> Seq.sortByDescending (fun t -> t.Date)
            |> Seq.toList
        
        
    module ImportCryptoCommandFactory =
        
        let create (filename:string) contents userId : ImportCryptoCommand =
            match filename with
            | x when x.Contains("coinbasepro") -> ImportCoinbasePro{UserId=userId; Content=contents}
            | x when x.Contains("blockfi") -> ImportBlockFi{UserId=userId; Content=contents}
            | _ -> ImportCoinbase{UserId=userId; Content=contents}
        
    type Handler(portfolio:IPortfolioStorage,accounts:IAccountStorage,crypto:ICryptoService,csvWriter:ICSVWriter,csvParser:ICSVParser) =
        
        let buy (data:CryptoTransaction) (crypto:OwnedCrypto) =
            crypto.Purchase(data.Quantity,data.DollarAmount,data.Date,data.Notes)
            
        let sell (data:CryptoTransaction) (crypto:OwnedCrypto) =
            crypto.Sell(data.Quantity,data.DollarAmount,data.Date,data.Notes)
            
        let reward (data:CryptoTransaction) (crypto:OwnedCrypto) =
            crypto.Reward(data.Quantity,data.DollarAmount,data.Date,data.Notes)
            
        let ``yield`` (data:CryptoTransaction) (crypto:OwnedCrypto) =
            crypto.Yield(data.Quantity,data.DollarAmount,data.Date,data.Notes)
            
        interface IApplicationService
        
        member _.Handle(cmd:CryptoTransactionWithUserId) = task {
            
            let data,userId,func,isSell =
                match cmd with
                | Buy (data,userId) -> (data,userId,buy,false)
                | Sell (data,userId) -> (data,userId,sell,true)
                | Reward (data,userId) -> (data,userId,reward,false)
                | Yield (data,userId) -> (data,userId,``yield``,false)
                
            let! user = accounts.GetUser(userId)
            match user with
            | None -> return ResponseUtils.failed "User not found"
            | _ ->
                let! crypto = portfolio.GetCrypto data.Token.Value userId
                
                if crypto = null && isSell then
                    return ResponseUtils.failed "Cannot sell crypto that is not owned"
                else            
                    let cryptoToUse =
                        match crypto with
                        | null -> OwnedCrypto(data.Token, userId |> IdentifierHelper.getUserId)
                        | _ -> crypto
                        
                    func data cryptoToUse
                    
                    do! portfolio.SaveCrypto cryptoToUse userId
                
                    return Ok
        }
        
        member _.Handle(query:DashboardQuery) = task {
            
            let! user = accounts.GetUser(query.UserId)
            match user with
            | None ->
                return ResponseUtils.failedTyped<CryptoDashboardView> "User not found"
            | _ ->
                let! cryptos = portfolio.GetCryptos(query.UserId)
                 
                let owned =
                    cryptos
                    |> Seq.filter (fun c -> c.State.Quantity > 0m)
                    |> Seq.map (fun c -> OwnedCryptoView(c.State))
                    |> Seq.sortByDescending (fun c -> c.Cost)
                    |> Seq.toList

                return CryptoDashboardView(owned) |> ResponseUtils.success<CryptoDashboardView>
        }
        
        member _.Handle(cmd:DeleteTransaction) = task {
            
            let! user = accounts.GetUser(cmd.UserId)
            match user with
            | None -> return ResponseUtils.failed "User not found"
            | _ ->
                let! crypto = portfolio.GetCrypto cmd.Token.Value cmd.UserId
                crypto.DeleteTransaction(cmd.TransactionId)
                do! portfolio.SaveCrypto crypto cmd.UserId
                return Ok
        }
        
        member _.Handle(query:Details) = task {
            
            let! prices = crypto.GetAll()
            return
                match prices.TryGet(query.Token) with
                | Some price -> CryptoDetailsView(query.Token, price) |> ResponseUtils.success<CryptoDetailsView>
                | None -> $"Price not found for {query.Token.ToString()}" |> ResponseUtils.failedTyped<CryptoDetailsView>
        }
        
        member _.Handle(query:Export) = task {
            
            let! user = accounts.GetUser(query.UserId)
            match user with
            | None -> return ResponseUtils.failedTyped<ExportResponse> "User not found"
            | _ ->
                let! cryptos = portfolio.GetCryptos(query.UserId)
                
                let filename = CSVExport.generateFilename "cryptos"
                let csv = CSVExport.cryptos csvWriter cryptos
                
                return ExportResponse(filename, csv) |> ResponseUtils.success<ExportResponse>
        }
        
        member this.Handle(cmd:ImportBlockFi) = task {
            
            let createCommand transactionType quantity date token userId =
                
                let award() =
                    Some (Reward({Quantity = quantity; DollarAmount = 0m; Date = date; Token = token; Notes = null}, userId))
                 
                match transactionType with
                | "cc rewards redemption" ->
                    award()
                | "interest payment" ->
                    award()
                | _ -> None
                
            let! user = accounts.GetUser(cmd.UserId)
            match user with
            | None -> return ResponseUtils.failed "User not found"
            | _ ->
                let parserResponse = csvParser.Parse<{|TransactionType:string; Cryptocurrency:string; Amount:decimal; ConfirmedAt:DateTimeOffset|}>(cmd.Content)
                
                match parserResponse.Success with
                | None -> return parserResponse |> ResponseUtils.toOkOrError
                | Some response  ->
                    let! commands =
                        response
                        |> Seq.map(fun obj -> createCommand (obj.TransactionType.ToLower()) obj.Amount obj.ConfirmedAt (Token obj.Cryptocurrency) cmd.UserId)
                        |> Seq.choose id
                        |> Seq.map( fun cmd -> this.Handle(cmd) |> Async.AwaitTask)
                        |> Async.Sequential
                        |> Async.StartAsTask
                        
                    return commands |> ResponseUtils.toOkOrConcatErrors
        }
        
        member this.Handle(cmd:ImportCoinbase) = task {
            
            let transactionData timestamp dollarAmount quantity token notes =
                {Quantity = quantity; DollarAmount = dollarAmount; Date = timestamp; Token = token; Notes = notes }
            
            let createCommand transactionType date dollarAmount quantity token notes userId =
                
                let data = transactionData date dollarAmount quantity token notes
                 
                match transactionType with
                | "buy" -> Buy(data, userId) |> Some
                | "sell" -> Sell(data, userId) |> Some
                | "coinbase earn" -> Reward(data, userId) |> Some
                | "rewards income" -> Yield(data, userId) |> Some
                | _ -> None
            
            
            let! user = accounts.GetUser(cmd.UserId)
            match user with
            | None -> return ResponseUtils.failed "User not found"
            | _ ->
                let parserResponse = csvParser.Parse<{|Timestamp:DateTimeOffset; TransactionType:string; Asset:string; QuantityTransacted:decimal; USDSubtotal:decimal|}>(cmd.Content)
                
                match parserResponse.Success with
                | None -> return parserResponse |> ResponseUtils.toOkOrError
                | Some response ->
                    let! commands =
                        response
                        |> Seq.map(fun obj -> createCommand obj.TransactionType obj.Timestamp obj.USDSubtotal obj.QuantityTransacted (Token obj.Asset) null cmd.UserId)
                        |> Seq.choose id
                        |> Seq.map( fun cmd -> this.Handle(cmd) |> Async.AwaitTask)
                        |> Async.Sequential
                        |> Async.StartAsTask
                        
                    return commands |> ResponseUtils.toOkOrConcatErrors
        }
        
        member this.Handle(cmd:ImportCoinbasePro) = task {
            
            let transactionData timestamp dollarAmount quantity token =
                {Quantity = quantity; DollarAmount = dollarAmount; Date = timestamp; Token = token; Notes = null }
                
            let toBuy (obj:TransactionGroup) =
                let data = transactionData obj.Date obj.DollarAmount obj.Quantity obj.Token
                Buy(data, cmd.UserId)
            
            let toSell (obj:TransactionGroup) =
                let data = transactionData obj.Date obj.DollarAmount obj.Quantity obj.Token
                Sell(data, cmd.UserId)
                
            let! user = accounts.GetUser(cmd.UserId)
            match user with
            | None -> return ResponseUtils.failed "User not found"
            | _ ->
                let parserResponse = csvParser.Parse<CoinbaseProRecord>(cmd.Content)
                
                match parserResponse.Success with
                | None -> return parserResponse |> ResponseUtils.toOkOrError
                | Some response ->
                    let container = CoinbaseProContainer(response)
                    
                    let! _ =
                        container.Buys
                        |> Seq.map(fun obj -> obj |> toBuy |> this.Handle |> Async.AwaitTask)
                        |> Async.Sequential
                        |> Async.StartAsTask
                        
                    let! _ =
                        container.Sells
                        |> Seq.map(fun obj -> obj |> toSell |> this.Handle |> Async.AwaitTask)
                        |> Async.Sequential
                        |> Async.StartAsTask
                        
                    return Ok
        }
        
        member this.Handle(import:ImportCryptoCommand) =
            match import with
            | ImportBlockFi cmd -> this.Handle(cmd)
            | ImportCoinbase cmd -> this.Handle(cmd)
            | ImportCoinbasePro cmd -> this.Handle(cmd)
        
        member this.Handle(query:OwnershipQuery) = task {
            
            let! user = accounts.GetUser(query.UserId)
            match user with
            | None -> return ResponseUtils.failedTyped<CryptoOwnershipView> "User not found"
            | _ ->
                let! crypto = portfolio.GetCrypto query.Token.Value query.UserId
                
                match crypto with
                | null -> return ResponseUtils.failedTyped<CryptoOwnershipView> "Crypto not found"
                | _ -> return CryptoOwnershipView(crypto.State) |> ResponseUtils.success<CryptoOwnershipView>
        }