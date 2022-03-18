using System.Threading.Tasks;

namespace core.Adapters.Emails
{
    public interface IEmailService
    {
        Task Send(Recipient recipient, Sender sender, EmailTemplate template, object properties);
        Task Send(Recipient recipient, Sender sender, string subject, string body);
    }
}