using System.Threading.Tasks;

namespace core.Shared.Adapters.SMS
{
    public interface ISMSClient
    {
        Task SendSMS(string message);   
    }
}