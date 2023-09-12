using System.Threading.Tasks;
using core.Shared.Adapters.Emails;
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

        public Task Send(Recipient recipient, Sender sender, string subject, string body)
        {
            return _sendGridClient switch {
                not null => SendWithClient(recipient, sender, subject, body),
                null => SendWithoutClient(recipient, sender, subject, body)
            };
        }

        private Task SendWithoutClient(Recipient recipient, Sender sender, string subject, string body)
        {
            _logger?.LogInformation($"Dummy send");
            
            return Task.CompletedTask;
        }

        private async Task SendWithClient(Recipient recipient, Sender sender, string subject, string body)
        {
            var fromAddr = new EmailAddress(email: sender.Email, name: sender.Name);
            var toAddr = new EmailAddress(email: recipient.Email, name: recipient.Name);
            var msg = MailHelper.CreateSingleEmail(fromAddr, toAddr, subject, body, null);

            await SendAndLog(msg);
        }

        private async Task SendAndLog(SendGridMessage msg)
        {
            var response = await _sendGridClient.SendEmailAsync(msg);

            var responseBody = await response.Body.ReadAsStringAsync();

            _logger.LogInformation("Sendgrid status {statusCode} with body: {responseBody}", response.StatusCode, responseBody);
        }

        public Task Send(
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
            _logger?.LogInformation("Dummy send with template {templateId}", template.Id);

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
