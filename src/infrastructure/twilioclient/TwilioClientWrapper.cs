using core.Shared.Adapters.SMS;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace twilioclient;
public class TwilioClientWrapper : ISMSClient
{
    private PhoneNumber _fromPhoneNumber;
    private PhoneNumber _toPhoneNumber;

    public TwilioClientWrapper(string accountSid, string authToken, string fromPhoneNumber, string toPhoneNumber)
    {
         TwilioClient.Init(accountSid, authToken);

        _fromPhoneNumber = new Twilio.Types.PhoneNumber(fromPhoneNumber);
        _toPhoneNumber = new Twilio.Types.PhoneNumber(toPhoneNumber);
    }

    public Task SendSMS(string message)
    {
        // Send SMS
        Console.WriteLine($"Sending SMS to {_toPhoneNumber}: {message}");
        
        var response = MessageResource.Create(
            body: message,
            from: _fromPhoneNumber,
            to: _toPhoneNumber
        );

        Console.WriteLine("Response from twilio: " + response.ToString());

        return Task.CompletedTask;
    }
}
