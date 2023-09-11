namespace core.fs.Options

    open core
    open core.Options
    open core.fs

    type OptionTransactionHandler(storage:IPortfolioStorage) =
        
        let createNote userId ``when`` notes (ticker:string) = task {
            
            match notes with
            | null -> return ()
            | "" -> return ()
            | _ ->
                let note = core.Notes.Note(userId=userId, ``created``=``when``, note=notes, ticker=ticker)
                let! _ = storage.Save(note, userId)
                return ()
        }
        
        interface IApplicationService
            
        member _.HandleSell (sell:OptionSold) = task {
            
            let! o = storage.GetOwnedOption(sell.AggregateId, sell.UserId)
            
            match o with
            | null ->
                return ()
            | _ ->
                let ``when`` = sell.When
                let notes = sell.Notes
                let! _ = createNote sell.UserId ``when`` notes (o.State.Ticker)
                return ()
        }
        
        member _.HandleBuy (buy:OptionPurchased) = task {
            
            let! o = storage.GetOwnedOption(buy.AggregateId, buy.UserId)
            
            match o with
            | null ->
                return ()
            | _ ->
                let ``when`` = buy.When
                let notes = buy.Notes
                let! _ = createNote buy.UserId ``when`` notes (o.State.Ticker)
                return ()
        }
            