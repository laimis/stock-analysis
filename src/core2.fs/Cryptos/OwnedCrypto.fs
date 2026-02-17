namespace core.Cryptos

open System
open System.Collections.Generic
open System.Linq
open core.Shared

type OwnedCrypto =
    inherit Aggregate<OwnedCryptoState>

    new (events: IEnumerable<AggregateEvent>) = { inherit Aggregate<OwnedCryptoState>(events) }

    new (token: Token, userId: Guid) as this =
        { inherit Aggregate<OwnedCryptoState>() }
        then
            if userId = Guid.Empty then
                raise (InvalidOperationException("Missing user id"))
            this.Apply(CryptoObtained(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, token, userId))

    member this.Purchase(quantity: decimal, dollarAmountSpent: decimal, date: DateTimeOffset, ?notes: string) =
        if quantity <= 0m then
            raise (InvalidOperationException("Price cannot be empty or zero"))

        if dollarAmountSpent <= 0m then
            raise (InvalidOperationException("Price cannot be empty or zero"))

        if date = DateTime.MinValue then
            raise (InvalidOperationException("Purchase date not specified"))

        this.Apply(
            CryptoPurchased(
                Guid.NewGuid(),
                this.State.Id,
                date,
                this.State.UserId,
                this.State.Token,
                quantity,
                dollarAmountSpent,
                defaultArg notes null
            )
        )

    member this.Reward(quantity: decimal, dollarAmountWorth: decimal, date: DateTimeOffset, notes: string) =
        if quantity < 0m then
            raise (InvalidOperationException("Quantity cannot be negative quantity"))

        if dollarAmountWorth < 0m then
            raise (InvalidOperationException("dollar amount worth cannot be negative"))

        this.Apply(
            CryptoAwarded(
                id = Guid.NewGuid(),
                aggregateId = this.State.Id,
                ``when`` = date,
                userId = this.State.UserId,
                token = this.State.Token,
                quantity = quantity,
                dollarAmountWorth = dollarAmountWorth,
                notes = notes
            )
        )

    member this.Yield(quantity: decimal, dollarAmountWorth: decimal, date: DateTimeOffset, notes: string) =
        if quantity < 0m then
            raise (InvalidOperationException("Quantity cannot be negative quantity"))

        if dollarAmountWorth < 0m then
            raise (InvalidOperationException("dollar amount worth cannot be negative"))

        this.Apply(
            CryptoYielded(
                id = Guid.NewGuid(),
                aggregateId = this.State.Id,
                ``when`` = date,
                userId = this.State.UserId,
                token = this.State.Token,
                quantity = quantity,
                dollarAmountWorth = dollarAmountWorth,
                notes = notes
            )
        )

    member this.DeleteTransaction(transactionId: Guid) =
        if this.State.BuyOrSell.All(fun t -> t.Id <> transactionId) then
            raise (InvalidOperationException("Unable to find transcation to delete using id " + string transactionId))

        this.Apply(
            CryptoTransactionDeleted(
                Guid.NewGuid(),
                this.State.Id,
                transactionId,
                DateTimeOffset.UtcNow
            )
        )

    member this.Delete() =
        this.Apply(
            CryptoDeleted(
                Guid.NewGuid(),
                this.State.Id,
                DateTimeOffset.UtcNow
            )
        )

    member this.Sell(quantity: decimal, dollarAmountReceived: decimal, date: DateTimeOffset, ?notes: string) =
        if quantity > this.State.Quantity then
            raise (InvalidOperationException("Amount owned is less than what is desired to sell"))

        this.Apply(
            CryptoSold(
                Guid.NewGuid(),
                this.State.Id,
                date,
                this.State.UserId,
                this.State.Token,
                quantity,
                dollarAmountReceived,
                defaultArg notes null
            )
        )
