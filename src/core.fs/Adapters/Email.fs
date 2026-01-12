namespace core.fs.Adapters.Email

open System.ComponentModel.DataAnnotations
open System.Threading.Tasks

[<CLIMutable>]
type EmailInput =
    {
        [<Required>]
        From: string
        [<Required>]
        FromName: string
        [<Required>]
        Subject: string
        PlainBody: string
        HtmlBody: string
        [<Required>]
        To: string
    }
    
[<Struct>]
type Recipient(email:string, name:string) =
    member _.Email = email
    member _.Name = name

[<Struct>]
type Sender(email:string, name:string) =
    static member NoReply = Sender("noreply@nightingaletrading.com", "Nightingale Trading")
    static member Support = Sender("noreply@nightingaletrading.com", "Nightingale Trading")
    
    member _.Email = email
    member _.Name = name
    
    
module EmailSettings =
    // TODO: these should be config file at the very least
    let PasswordResetUrl = "https://www.nightingaletrading.com/profile/passwordreset"
    let ConfirmAccountUrl = "https://www.nightingaletrading.com/api/account/confirm"
    
type IEmailService =
    abstract SendUserDeleted : recipient:Recipient -> sender:Sender -> properties:obj -> Task<Result<unit, string>>
    abstract SendWelcome : recipient:Recipient -> sender:Sender -> properties:obj -> Task<Result<unit, string>>
    abstract SendContactUs : recipient:Recipient -> sender:Sender -> properties:obj -> Task<Result<unit, string>>
    abstract SendVerify : recipient:Recipient -> sender:Sender -> properties:obj -> Task<Result<unit, string>>
    abstract SendPasswordReset : recipient:Recipient -> sender:Sender -> properties:obj -> Task<Result<unit, string>>
    abstract SendAlerts : recipient:Recipient -> sender:Sender -> properties:obj -> Task<Result<unit, string>>
    abstract SendBrokerageTransactions : recipient:Recipient -> sender:Sender -> properties:obj -> Task<Result<unit, string>>
    abstract SendSellAlert : recipient:Recipient -> sender:Sender -> properties:obj -> Task<Result<unit, string>>
    abstract SendMaxProfits : recipient:Recipient -> sender:Sender -> properties:obj -> Task<Result<unit, string>>
    abstract Send : recipient:Recipient -> sender:Sender -> subject:string -> plainTextBody:string -> htmlBody:string -> Task<Result<unit, string>>
    abstract SendWithInput : input:EmailInput -> Task<Result<unit, string>>