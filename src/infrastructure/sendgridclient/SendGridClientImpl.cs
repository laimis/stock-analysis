using System;
using System.Threading.Tasks;
using core.Adapters.Emails;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace sendgridclient
{
    public class SendGridClientImpl : IEmailService
    {
        private string _key;
        private const string NO_REPLY = "noreply@nightingaletrading.com";

        public SendGridClientImpl(string apiKey)
        {
            _key = apiKey;
        }

        public async Task Send(string to, Sender sender, string subject, string body)
        {
            var client = new SendGridClient(_key);
            var fromAddr = new EmailAddress(sender.Email, sender.Name);
            var toAddr = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(fromAddr, toAddr, subject, body, null);
            
            var response = await client.SendEmailAsync(msg);

            Console.WriteLine("Sendgrid response: " + response.StatusCode);
            
            var err = await response.Body.ReadAsStringAsync();

            Console.WriteLine("Sendgrid response: " + err);
        }

        public async Task Send(
            string recipient,
            Sender sender,
            EmailTemplate template,
            object properties)
        {
            var client = new SendGridClient(_key);
            var from = new EmailAddress(sender.Email, sender.Name);
            var to = new EmailAddress(recipient);
            
            var msg = MailHelper.CreateSingleTemplateEmail(
                from, to, template.Id, properties
            );
            
            var response = await client.SendEmailAsync(msg);

            var err = await response.Body.ReadAsStringAsync();

            Console.WriteLine($"Sendgrid status {response.StatusCode} with body: {err}");
        }
    }
}
