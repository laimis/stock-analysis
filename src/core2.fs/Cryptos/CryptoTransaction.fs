namespace core.Cryptos

open System
open core.Shared

[<AllowNullLiteral>]
type CryptoTransaction(aggregateId: Guid, transactionId: Guid, token: string, description: string, price: decimal, debit: decimal, credit: decimal, ``when``: DateTimeOffset) =
    member val AggregateId = aggregateId with get
    member val TransactionId = transactionId with get
    member val Token = token with get
    member val Description = description with get
    member val Price = price with get
    member val Debit = debit with get
    member val Credit = credit with get
    member val When = ``when`` with get

    static member internal DebitTx(aggregateId: Guid, transactionId: Guid, token: string, description: string, price: decimal, dollarAmount: decimal, ``when``: DateTimeOffset) =
        CryptoTransaction(
            aggregateId,
            transactionId,
            token,
            description,
            price,
            dollarAmount,
            0m,
            ``when``
        )

    static member internal CreditTx(aggregateId: Guid, transactionId: Guid, token: string, description: string, price: decimal, dollarAmount: decimal, ``when``: DateTimeOffset) =
        CryptoTransaction(
            aggregateId,
            transactionId,
            token,
            description,
            price,
            0m,
            dollarAmount,
            ``when``
        )

    member this.ToSharedTransaction() =
        match this.Credit with
        | c when c > 0m -> Transaction.NonPLTx(this.AggregateId, this.TransactionId, Ticker(this.Token), this.Description, this.Price, this.Credit, this.When, isOption = false)
        | _ -> Transaction.NonPLTx(this.AggregateId, this.TransactionId, Ticker(this.Token), this.Description, this.Price, this.Debit, this.When, isOption = false)
