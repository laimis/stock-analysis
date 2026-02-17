namespace core.Cryptos

open System

[<Struct>]
type Token =
    val private _symbol: string
    
    new (symbol: string) =
        if String.IsNullOrWhiteSpace(symbol) then
            raise (ArgumentException("Symbol cannot be blank", "symbol"))
        { _symbol = symbol.ToUpper() }

    static member op_Implicit(t: Token) : string = t._symbol
    static member op_Implicit(t: string) : Token = Token(t)

    member this.Value = this._symbol
