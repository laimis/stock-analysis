namespace core.fs.Cryptos.Import

open System

type CoinbaseProRecord =
    {
        Type:string
        AmountBalanceUnit:string
        Amount:Nullable<decimal>
        Time:DateTimeOffset
        TradeId:string
        OrderId:string
    }
    
type TransactionGroup =
    val IsBuy : bool
    val IsSell : bool
    val DollarAmount : decimal
    val Quantity: decimal
    val Token: core.Cryptos.Token
    val Date: DateTimeOffset
    
    new (group:string * seq<CoinbaseProRecord>) =
        let (_, recordSequence) = group
        let records = recordSequence |> Seq.toList
        
        if records.Length > 2 then
            raise (InvalidOperationException("More than two records found for " + records[0].TradeId))
        
        let isBuy = records[0].AmountBalanceUnit = "USD"
        
        {
            IsBuy = isBuy
            IsSell = not isBuy
            DollarAmount =
                match isBuy with
                | true -> Math.Abs(records[0].Amount.Value)
                | false -> records[1].Amount.Value
            Quantity =
                match isBuy with
                | true -> records[1].Amount.Value
                | false -> Math.Abs(records[0].Amount.Value)
            Token =
                match isBuy with
                | true -> core.Cryptos.Token(records[1].AmountBalanceUnit)
                | false -> core.Cryptos.Token(records[0].AmountBalanceUnit)
            Date = records[0].Time
        }
        
type CoinbaseProContainer(records:CoinbaseProRecord seq) =
    
    member _.Transactions = 
        records
        |> Seq.groupBy(fun r -> r.TradeId)
        |> Seq.map(fun g -> TransactionGroup(g))
        |> Seq.toList
        
    member this.Buys =
        this.Transactions
        |> Seq.filter(fun t -> t.IsBuy)
        
    member this.Sells =
        this.Transactions
        |> Seq.filter(fun t -> t.IsSell)