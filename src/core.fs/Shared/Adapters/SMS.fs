namespace core.fs.Shared.Adapters.SMS

open System.Threading.Tasks

type ISMSClient =
    abstract SendSMS : message:string -> Task
    abstract TurnOff : unit -> unit
    abstract TurnOn : unit -> unit
    abstract IsOn : bool