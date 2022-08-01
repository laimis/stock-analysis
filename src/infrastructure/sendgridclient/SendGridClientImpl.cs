using System;
using System.Threading.Tasks;
using core.Adapters.Emails;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace sendgridclient
{
    public class SendGridClientImpl : IEmailService
    {
        private SendGridClient _sendGridClient;
        private const string NO_REPLY = "noreply@nightingaletrading.com";

        public SendGridClientImpl(string apiKey)
        {
            if (apiKey != null)
            {
                _sendGridClient = new SendGridClient(apiKey);
            }
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
            // TODO: replace with logging
            // Console.WriteLine($"Sending email to {recipient.Email} with subject {subject} and body {body}");
            return Task.CompletedTask;
        }

        private async Task SendWithClient(Recipient recipient, Sender sender, string subject, string body)
        {
            var fromAddr = new EmailAddress(email: sender.Email, name: sender.Name);
            var toAddr = new EmailAddress(email: recipient.Email, name: recipient.Name);
            var msg = MailHelper.CreateSingleEmail(fromAddr, toAddr, subject, body, null);
            
            var response = await _sendGridClient.SendEmailAsync(msg);

            Console.WriteLine("Sendgrid response: " + response.StatusCode);
            
            var responseBody = await response.Body.ReadAsStringAsync();

            Console.WriteLine("Sendgrid response: " + responseBody);
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
            Console.WriteLine($"Sending email to {recipient} with template {template.Id} and body {JsonConvert.SerializeObject(properties)}");
            return Task.CompletedTask;
        }

        private async Task SendWithClient(Recipient recipient, Sender sender, EmailTemplate template, object properties)
        {
            var from = new EmailAddress(email: sender.Email, name: sender.Name);
            var to = new EmailAddress(email: recipient.Email, name: recipient.Name);
            
            var msg = MailHelper.CreateSingleTemplateEmail(
                from, to, template.Id, properties
            );
            
            var response = await _sendGridClient.SendEmailAsync(msg);

            var responseBody = await response.Body.ReadAsStringAsync();

            Console.WriteLine($"Sendgrid status {response.StatusCode} with body: {responseBody}");
        }
    }
}
