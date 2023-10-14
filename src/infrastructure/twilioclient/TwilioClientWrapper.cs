using core.fs.Shared.Adapters.SMS;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace twilioclient;
public class TwilioClientWrapper : ISMSClient
{
    private readonly PhoneNumber? _fromPhoneNumber;
    private readonly PhoneNumber? _toPhoneNumber;
    private readonly bool _configured;
    private readonly ILogger<TwilioClientWrapper> _logger;

    public bool IsOn { get; private set; }

    public TwilioClientWrapper(
        string accountSid,
        string authToken,
        string fromPhoneNumber,
        ILogger<TwilioClientWrapper> logger,
        string toPhoneNumber)
    {
        _logger = logger;
        
        // check for all params to be not null
        if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhoneNumber) || string.IsNullOrEmpty(toPhoneNumber))
        {
            return;
        }

        TwilioClient.Init(accountSid, authToken);

        _fromPhoneNumber = new PhoneNumber(fromPhoneNumber);
        _toPhoneNumber = new PhoneNumber(toPhoneNumber);
        _configured = true;
        IsOn = true;
    }

    public Task SendSMS(string message)
    {
        return (_configured && IsOn) switch {
            true => SendViaTwilio(message),
            false => ToLogger(message)
        };
    }

    private Task ToLogger(string message)
    {
        _logger.LogInformation($"Sending SMS: {message}");
        return Task.CompletedTask;
    }

    private Task SendViaTwilio(string message)
    {
        // Send SMS
        _logger.LogInformation($"Sending SMS to {_toPhoneNumber}: {message}");
        
        var response = MessageResource.Create(
            body: message,
            from: _fromPhoneNumber,
            to: _toPhoneNumber
        );

        _logger.LogInformation("Response from twilio: " + response.ToString());

        return Task.CompletedTask;
    }

    public void TurnOff() => IsOn = false;
    public void TurnOn() => IsOn = true;
}
