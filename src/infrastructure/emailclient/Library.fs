namespace emailclient

open System.Threading.Tasks
open core.fs.Adapters.Email

/// Amazon SES email service implementation
/// Configuration parameters needed:
/// - AWS_REGION: AWS region for SES (e.g., "us-east-1")
/// - AWS_ACCESS_KEY_ID: AWS access key
/// - AWS_SECRET_ACCESS_KEY: AWS secret key
/// Or use IAM role-based authentication
type SESEmailService(accessKeyId: string, secretAccessKey: string, region: string) =
    
    // TODO: Initialize AWS SES client once SDK is added
    // private val sesClient = AmazonSimpleEmailServiceClient(...)
    
    /// Internal helper to send templated emails
    /// Will be implemented with SES SendTemplatedEmail API
    let sendTemplated (recipient: Recipient) (sender: Sender) (templateName: string) (templateData: obj) =
        task {
            // TODO: Implement SES templated email sending
            // 1. Serialize templateData to JSON
            // 2. Create SendTemplatedEmailRequest
            // 3. Call sesClient.SendTemplatedEmailAsync
            printfn "[SES] Would send templated email '%s' to %s" templateName recipient.Email
            return ()
        }
    
    /// Internal helper to send raw emails
    /// Will be implemented with SES SendEmail API
    let sendRaw (recipient: Recipient) (sender: Sender) (subject: string) (plainText: string) (html: string) =
        task {
            // TODO: Implement SES raw email sending
            // 1. Create Message with subject and body (text/html)
            // 2. Create SendEmailRequest
            // 3. Call sesClient.SendEmailAsync
            printfn "[SES] Would send email '%s' to %s from %s" subject recipient.Email sender.Email
            return ()
        }
    
    interface IEmailService with
        
        member _.SendUserDeleted recipient sender properties =
            sendTemplated recipient sender "UserDeleted" properties
        
        member _.SendWelcome recipient sender properties =
            sendTemplated recipient sender "Welcome" properties
        
        member _.SendContactUs recipient sender properties =
            sendTemplated recipient sender "ContactUs" properties
        
        member _.SendVerify recipient sender properties =
            sendTemplated recipient sender "Verify" properties
        
        member _.SendPasswordReset recipient sender properties =
            sendTemplated recipient sender "PasswordReset" properties
        
        member _.SendAlerts recipient sender properties =
            sendTemplated recipient sender "Alerts" properties
        
        member _.SendBrokerageTransactions recipient sender properties =
            sendTemplated recipient sender "BrokerageTransactions" properties
        
        member _.SendSellAlert recipient sender properties =
            sendTemplated recipient sender "SellAlert" properties
        
        member _.SendMaxProfits recipient sender properties =
            sendTemplated recipient sender "MaxProfits" properties
        
        member _.Send recipient sender subject plainTextBody htmlBody =
            sendRaw recipient sender subject plainTextBody htmlBody
        
        member _.SendWithInput input =
            let recipient = Recipient(input.To, "")
            let sender = Sender(input.From, input.FromName)
            sendRaw recipient sender input.Subject input.PlainBody input.HtmlBody
