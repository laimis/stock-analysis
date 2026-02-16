namespace twilioclient

open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Twilio
open Twilio.Rest.Api.V2010.Account
open Twilio.Types
open core.fs.Adapters.SMS

type TwilioClientWrapper(
    accountSid: string option,
    authToken: string option,
    fromPhoneNumber: string option,
    logger: ILogger<TwilioClientWrapper>,
    toPhoneNumber: string option) =
    
    let mutable isOn = false
    let configured, fromPhone, toPhone =
        match accountSid, authToken, fromPhoneNumber, toPhoneNumber with
        | Some sid, Some token, Some fromNum, Some toNum when 
            not (System.String.IsNullOrEmpty(sid)) &&
            not (System.String.IsNullOrEmpty(token)) &&
            not (System.String.IsNullOrEmpty(fromNum)) &&
            not (System.String.IsNullOrEmpty(toNum)) ->
            TwilioClient.Init(sid, token)
            isOn <- true
            (true, Some (PhoneNumber(fromNum)), Some (PhoneNumber(toNum)))
        | _ -> (false, None, None)
    
    member _.IsOn with get() = isOn
    
    member _.TurnOff() = isOn <- false
    
    member _.TurnOn() = isOn <- true
    
    member _.SendSMS(message: string) : Task =
        if configured && isOn then
            logger.LogInformation($"Sending SMS to {toPhone.Value}: {message}")
            
            let response = 
                MessageResource.Create(
                    body = message,
                    from = fromPhone.Value,
                    ``to`` = toPhone.Value
                )
            
            logger.LogInformation("Response from twilio: " + response.ToString())
            Task.CompletedTask
        else
            logger.LogInformation($"Sending SMS: {message}")
            Task.CompletedTask
    
    interface ISMSClient with
        member this.SendSMS(message: string) = this.SendSMS(message)
        member this.TurnOff() = this.TurnOff()
        member this.TurnOn() = this.TurnOn()
        member this.IsOn = this.IsOn
