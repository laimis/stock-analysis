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

        public async Task Send(string to, string from, string subject, string body)
        {
            var client = new SendGridClient(_key);
            var fromAddr = new EmailAddress(from);
            var toAddr = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(fromAddr, toAddr, subject, body, null);
            
            var response = await client.SendEmailAsync(msg);

            Console.WriteLine("Sendgrid response: " + response.StatusCode);
            
            var err = await response.Body.ReadAsStringAsync();

            Console.WriteLine("Sendgrid response: " + err);
        }

        public async Task Send(string email, string templateId, object properties)
        {
            var client = new SendGridClient(_key);
            var from = new EmailAddress(NO_REPLY);
            var to = new EmailAddress(email);
            
            var msg = MailHelper.CreateSingleTemplateEmail(
                from, to, templateId, properties
            );
            
            var response = await client.SendEmailAsync(msg);

            Console.WriteLine("Sendgrid response: " + response.StatusCode);
        }
    }
}
