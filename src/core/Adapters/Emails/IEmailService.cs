using System.Threading.Tasks;

namespace core.Adapters.Emails
{
    public interface IEmailService
    {
        Task Send(string recipient, EmailSender sender, EmailTemplate template, object properties);
        Task Send(string to, EmailSender sender, string subject, string body);
    }
}