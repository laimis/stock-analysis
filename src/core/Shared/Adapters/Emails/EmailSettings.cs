using System.ComponentModel.DataAnnotations;

namespace core.Shared.Adapters.Emails
{
    public class EmailInput
    {
        [Required]
        public string From { get; set; }
        [Required]
        public string FromName { get; set; }
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Body { get; set; }
        [Required]
        public string To { get; set; }
    }
    
    public static class EmailSettings
    {
        public const string PasswordResetUrl = "https://www.nightingaletrading.com/profile/passwordreset";
        public const string ConfirmAccountUrl = "https://www.nightingaletrading.com/api/account/confirm";
    }

    public struct Recipient
    {
        public Recipient(string email, string name)
        {
            Email = email;
            Name = name;
        }

        public string Email { get; set; }
        public string Name { get; set; }
    }

    public readonly struct Sender
    {
        public static readonly Sender NoReply = new("noreply@nightingaletrading.com", "Nightingale Trading");
        public static readonly Sender Support = new("support@nightingaletrading.com", "Nightingale Trading");

        public Sender(string email, string name)
        {
            Email = email;
            Name = name;
        }

        public string Email { get; }
        public string Name { get; }
    }

    public readonly struct EmailTemplate
    {
        public static readonly EmailTemplate PasswordReset = new("d-6f4ac095859a417d88e8243d8056838b");
        public static readonly EmailTemplate ConfirmAccount = new ("d-b67630e1e9684b1f8dd6dd8dd33cfff8");
        public static readonly EmailTemplate ReviewEmail = new ("d-b03188c3d1d74945adb7732b3684ef4b");

        public static readonly EmailTemplate AdminNewUser = new ("d-84d8de39b7084b23b9ab2ce6146dfbc3");
        public static readonly EmailTemplate AdminUserDeleted = new ("d-3bdfe7613d1048e5b1a469a155f945b0");
        public static readonly EmailTemplate AdminContact = new ("d-ab673f8ba0be40018dcc20aca9880d63");

        public static readonly EmailTemplate Alerts = new ("d-8022af23c4624a9ab54a0bb6ac5ce840");

        public static readonly EmailTemplate NewUserWelcome = new ("d-7fe69a3b1f164830b82112dc10c745b0");

        public static readonly EmailTemplate SellAlert = new ("d-a8ac152d334d471aaab2fead90a8f120");

        public EmailTemplate(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }
}