namespace core.Adapters.Emails
{
    public static class EmailSettings
    {
        public static readonly Recipient Admin = new Recipient(email: "laimis@gmail.com", name: null);
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

    public struct Sender
    {
        public static Sender NoReply = new Sender("noreply@nightingaletrading.com", "Nightingale Trading");
        public static Sender Support = new Sender("support@nightingaletrading.com", "Nightingale Trading");

        public Sender(string email, string name)
        {
            Email = email;
            Name = name;
        }

        public string Email { get; }
        public string Name { get; }
    }

    public struct EmailTemplate
    {
        public static EmailTemplate PasswordReset = new EmailTemplate("d-6f4ac095859a417d88e8243d8056838b");
        public static EmailTemplate ConfirmAccount = new EmailTemplate("d-b67630e1e9684b1f8dd6dd8dd33cfff8");
        public static EmailTemplate ReviewEmail = new EmailTemplate("d-b03188c3d1d74945adb7732b3684ef4b");

        public static EmailTemplate AdminNewUser = new EmailTemplate("d-84d8de39b7084b23b9ab2ce6146dfbc3");
        public static EmailTemplate AdminUserDeleted = new EmailTemplate("d-3bdfe7613d1048e5b1a469a155f945b0");
        public static EmailTemplate AdminContact = new EmailTemplate("d-ab673f8ba0be40018dcc20aca9880d63");

        public static EmailTemplate Alerts = new EmailTemplate("d-8022af23c4624a9ab54a0bb6ac5ce840");

        public static EmailTemplate NewUserWelcome = new EmailTemplate("d-7fe69a3b1f164830b82112dc10c745b0");

        public static EmailTemplate SellAlert = new EmailTemplate("d-a8ac152d334d471aaab2fead90a8f120");

        public EmailTemplate(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }
}