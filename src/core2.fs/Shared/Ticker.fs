namespace core.Shared

open System

[<Struct>]
[<CustomEquality; CustomComparison>]
type Ticker =
    val private _ticker: string
    
    new(value: string) =
        if String.IsNullOrWhiteSpace(value) then
            invalidArg "value" "Ticker cannot be blank"
        { _ticker = value.ToUpper() }
    
    member this.Value = this._ticker
    
    override this.ToString() = this._ticker
    
    override this.Equals(obj: obj) =
        match obj with
        | :? Ticker as other -> this._ticker = other._ticker
        | _ -> false
    
    override this.GetHashCode() =
        if isNull this._ticker then 0 else this._ticker.GetHashCode()
    
    interface IComparable with
        member this.CompareTo(obj: obj) =
            match obj with
            | :? Ticker as t -> String.CompareOrdinal(this._ticker, t._ticker)
            | _ -> -1
    
    interface IEquatable<Ticker> with
        member this.Equals(other: Ticker) =
            this._ticker = other._ticker
