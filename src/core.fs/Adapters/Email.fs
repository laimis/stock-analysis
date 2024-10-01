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
    // TODO: strange hardcode here
    static member NoReply = Sender("noreply@nightingaletrading.com", "Nightingale Trading")
    static member Support = Sender("noreply@nightingaletrading.com", "Nightingale Trading")
    
    member _.Email = email
    member _.Name = name
    
    
module EmailSettings =
    // TODO: these should be config file at the very least
    let PasswordResetUrl = "https://www.nightingaletrading.com/profile/passwordreset"
    let ConfirmAccountUrl = "https://www.nightingaletrading.com/api/account/confirm"

[<Struct>]
type EmailTemplate(id:string) =
    member _.Id = id
    
    static member PasswordReset = EmailTemplate("d-6f4ac095859a417d88e8243d8056838b")
    static member ConfirmAccount = EmailTemplate("d-b67630e1e9684b1f8dd6dd8dd33cfff8")
    static member ReviewEmail = EmailTemplate("d-b03188c3d1d74945adb7732b3684ef4b")
    
    static member AdminNewUser = EmailTemplate("d-84d8de39b7084b23b9ab2ce6146dfbc3")
    static member AdminUserDeleted = EmailTemplate("d-3bdfe7613d1048e5b1a469a155f945b0")
    static member AdminContact = EmailTemplate("d-ab673f8ba0be40018dcc20aca9880d63")
    
    static member Alerts = EmailTemplate("d-8022af23c4624a9ab54a0bb6ac5ce840")
    
    static member NewUserWelcome = EmailTemplate("d-7fe69a3b1f164830b82112dc10c745b0")
    
    static member SellAlert = EmailTemplate("d-a8ac152d334d471aaab2fead90a8f120")
    static member MaxProfits = EmailTemplate("d-b06887b67edc464489820284fcd81617")
    static member BrokerageTransactions = EmailTemplate("d-42661e4810af471f92c72d926289e2d5")
    
type IEmailService =
    abstract SendWithTemplate : recipient:Recipient -> sender:Sender -> template:EmailTemplate -> properties:System.Object -> Task
    abstract Send : recipient:Recipient -> sender:Sender -> subject:string -> plainTextBody:string -> htmlBody:string -> Task
    abstract SendWithInput : input:EmailInput -> Task
