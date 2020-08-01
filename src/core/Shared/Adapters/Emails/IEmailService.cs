using System.Threading.Tasks;

namespace core.Adapters.Emails
{
    public interface IEmailService
    {
        Task Send(string recipient, Sender sender, EmailTemplate template, object properties);
        Task Send(string to, Sender sender, string subject, string body);
    }
}