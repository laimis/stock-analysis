namespace emailclient

open core.fs.Adapters.Email
open Amazon.SimpleEmail
open Amazon.SimpleEmail.Model

type SESEmailService(accessKeyId: string, secretAccessKey: string, region: string) =

    let client = 
        let credentials = Amazon.Runtime.BasicAWSCredentials(accessKeyId, secretAccessKey)
        let config = AmazonSimpleEmailServiceConfig()
        config.RegionEndpoint <- Amazon.RegionEndpoint.GetBySystemName(region)
        new AmazonSimpleEmailServiceClient(credentials, config)

    let genericSend (request: SendEmailRequest) = task {
        try
            let! _ = client.SendEmailAsync(request)
            return Ok ()
        with ex ->
            return Error ex.Message
    }

    let supportSender = "noreply@nightingaletrading.com"
    let alertsSender = "alerts@nightingaletrading.com"
    let senderName = "Nightingale Trading"

    let toGenericList (list: string list) = 
        new System.Collections.Generic.List<string>(list)

    /// Send email using template with rendering
    let sendWithTemplate (recipient: Recipient) (senderEmail: string) (templateName: string) (subject: string) (payload: obj) = task {
        
        let! renderedContentResult = EmailTemplateManager.processTemplate templateName payload

        match renderedContentResult with
        | Error err ->
            failwith err
        
        | Ok content ->
        
            let request = SendEmailRequest()
            request.Source <- $"{senderName} <{senderEmail}>"
            
            let destination = Destination()
            destination.ToAddresses <- [recipient.Email] |> toGenericList
            request.Destination <- destination
            
            let messageContent = Content()
            messageContent.Data <- subject
            
            let message = Message()
            message.Subject <- messageContent
            
            let bodyContent = Content()
            bodyContent.Data <- content
            
            let body = Body()
            body.Html <- bodyContent
            
            message.Body <- body
            request.Message <- message
            
            let! result = genericSend request
            match result with
            | Ok () -> return ()
            | Error err -> failwith err
    }

    /// Send raw email (text and/or HTML)
    let sendRaw (recipient: Recipient) (sender: Sender) (subject: string) (plainText: string) (html: string) = task {
        
        // Validate that at least one body type is provided
        if System.String.IsNullOrWhiteSpace(html) && System.String.IsNullOrWhiteSpace(plainText) then
            failwith "Email must have either HTML or plain text body"
        
        let request = SendEmailRequest()
        request.Source <- $"{sender.Name} <{sender.Email}>"
        
        let destination = Destination()
        destination.ToAddresses <- [recipient.Email] |> toGenericList
        request.Destination <- destination
        
        let messageContent = Content()
        messageContent.Data <- subject
        
        let message = Message()
        message.Subject <- messageContent
        
        let body = Body()
        
        if not (System.String.IsNullOrWhiteSpace(html)) then
            let htmlContent = Content()
            htmlContent.Data <- html
            body.Html <- htmlContent
        
        if not (System.String.IsNullOrWhiteSpace(plainText)) then
            let textContent = Content()
            textContent.Data <- plainText
            body.Text <- textContent
        
        message.Body <- body
        request.Message <- message
        
        let! result = genericSend request
        match result with
        | Ok () -> return ()
        | Error err -> failwith err
    }

    interface IEmailService with
        
        member _.SendUserDeleted recipient sender properties =
            sendWithTemplate recipient supportSender "userdeleted" "Account Deleted" properties
        
        member _.SendWelcome recipient sender properties =
            sendWithTemplate recipient supportSender "welcome" "Welcome to Nightingale Trading!" properties
        
        member _.SendContactUs recipient sender properties =
            sendWithTemplate recipient supportSender "contactus" "Thank you for contacting us" properties
        
        member _.SendVerify recipient sender properties =
            sendWithTemplate recipient supportSender "verify" "Verify your account" properties
        
        member _.SendPasswordReset recipient sender properties =
            sendWithTemplate recipient supportSender "passwordreset" "Password Reset Instructions" properties
        
        member _.SendAlerts recipient sender properties =
            sendWithTemplate recipient alertsSender "alerts" "Stock Alerts" properties
        
        member _.SendBrokerageTransactions recipient sender properties =
            sendWithTemplate recipient supportSender "brokeragetransactions" "Brokerage Transaction Summary" properties
        
        member _.SendSellAlert recipient sender properties =
            sendWithTemplate recipient alertsSender "sellalert" "Sell Alert" properties
        
        member _.SendMaxProfits recipient sender properties =
            sendWithTemplate recipient alertsSender "maxprofits" "Maximum Profit Achieved" properties
        
        member _.Send recipient sender subject plainTextBody htmlBody =
            sendRaw recipient sender subject plainTextBody htmlBody
        
        member _.SendWithInput input =
            let recipient = Recipient(input.To, "")
            let sender = Sender(input.From, input.FromName)
            sendRaw recipient sender input.Subject input.PlainBody input.HtmlBody
