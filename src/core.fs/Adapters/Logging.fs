namespace core.fs.Adapters.Logging

type ILogger =
    abstract member LogInformation : string -> unit
    abstract member LogWarning : string -> unit
    abstract member LogError : string -> unit