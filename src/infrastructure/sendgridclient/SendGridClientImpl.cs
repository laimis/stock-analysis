using System.Threading.Tasks;
using core.fs.Adapters.Email;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace sendgridclient
{
    public class SendGridClientImpl : IEmailService
    {
        private readonly SendGridClient _sendGridClient;
        private readonly ILogger<SendGridClientImpl> _logger;

        public SendGridClientImpl(
            string apiKey,
            ILogger<SendGridClientImpl> logger)
        {
            if (apiKey != null)
            {
                _sendGridClient = new SendGridClient(apiKey);
            }

            _logger = logger;
        }

        public Task Send(Recipient recipient, Sender sender, string subject, string plainTextBody, string htmlBody)
        {
            return _sendGridClient switch {
                not null => SendWithClient(recipient, sender, subject, plainTextBody, htmlBody),
                null => SendWithoutClient(recipient, sender, subject, plainTextBody ?? htmlBody)
            };
        }

        private Task SendWithoutClient(Recipient recipient, Sender sender, string subject, string body)
        {
            _logger?.LogInformation($"Dummy send");
            
            return Task.CompletedTask;
        }

        private async Task SendWithClient(Recipient recipient, Sender sender, string subject, string plainTextContent, string htmlContent)
        {
            var fromAddr = new EmailAddress(email: sender.Email, name: sender.Name);
            var toAddr = new EmailAddress(email: recipient.Email, name: recipient.Name);
            var msg = MailHelper.CreateSingleEmail(fromAddr, toAddr, subject, plainTextContent, htmlContent);

            await SendAndLog(msg);
        }

        private async Task SendAndLog(SendGridMessage msg)
        {
            var response = await _sendGridClient.SendEmailAsync(msg);

            var responseBody = await response.Body.ReadAsStringAsync();

            _logger.LogInformation("Sendgrid status {statusCode} with body: {responseBody}", response.StatusCode, responseBody);
        }

        public Task SendWithInput(EmailInput obj) => Send(
            recipient: new Recipient(email: obj.To, name: null),
            sender: new Sender(obj.From, obj.FromName),
            subject: obj.Subject,
            plainTextBody: obj.PlainBody,
            htmlBody: obj.HtmlBody
        );

        public Task SendWithTemplate(
            Recipient recipient,
            Sender sender,
            EmailTemplate template,
            object properties)
        {
            return _sendGridClient switch {
                not null => SendWithClient(recipient, sender, template, properties),
                null => SendWithoutClient(recipient, sender, template, properties)
            };
        }

        private Task SendWithoutClient(Recipient recipient, Sender sender, EmailTemplate template, object properties)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(properties);
            
            _logger?.LogInformation("Dummy send with template {templateId}: {json}", template.Id, json);

            return Task.CompletedTask;
        }

        private async Task SendWithClient(Recipient recipient, Sender sender, EmailTemplate template, object properties)
        {
            var from = new EmailAddress(email: sender.Email, name: sender.Name);
            var to = new EmailAddress(email: recipient.Email, name: recipient.Name);
            
            var msg = MailHelper.CreateSingleTemplateEmail(
                from, to, template.Id, properties
            );

            await SendAndLog(msg);
        }
    }
}
