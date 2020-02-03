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

        public async Task Send(string email, string subject, string plain, string html)
        {
            var client = new SendGridClient(_key);
            var from = new EmailAddress(NO_REPLY);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plain, html);
            
            var response = await client.SendEmailAsync(msg);

            Console.WriteLine("Sendgrid response: " + response.StatusCode);
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
