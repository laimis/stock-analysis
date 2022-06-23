using core.Shared.Adapters.SMS;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace twilioclient;
public class TwilioClientWrapper : ISMSClient
{
    private PhoneNumber? _fromPhoneNumber = null;
    private PhoneNumber? _toPhoneNumber = null;
    private bool _configured;

    public TwilioClientWrapper(string accountSid, string authToken, string fromPhoneNumber, string toPhoneNumber)
    {
        // check for all params to be not null
        if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhoneNumber) || string.IsNullOrEmpty(toPhoneNumber))
        {
            return;
        }

        TwilioClient.Init(accountSid, authToken);

        _fromPhoneNumber = new Twilio.Types.PhoneNumber(fromPhoneNumber);
        _toPhoneNumber = new Twilio.Types.PhoneNumber(toPhoneNumber);
        _configured = true;
    }

    public Task SendSMS(string message)
    {
        return _configured switch {
            true => SendViaTwilio(message),
            false => ToConsole(message)
        };
    }

    private Task ToConsole(string message)
    {
        Console.WriteLine($"Sending SMS: {message}");
        return Task.CompletedTask;
    }

    private Task SendViaTwilio(string message)
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
