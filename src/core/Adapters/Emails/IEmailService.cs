using System.Threading.Tasks;

namespace core.Adapters.Emails
{
    public interface IEmailService
    {
         Task Send(string email, string templateId, object properties);
    }
}