namespace core.Adapters.Emails
{
    public static class EmailSettings
    {
        public const string Admin = "laimis@gmail.com";

        public const string TemplateUserDeleted = "d-3bdfe7613d1048e5b1a469a155f945b0";
        public const string TemplatePasswordReset = "d-6f4ac095859a417d88e8243d8056838b";
        public static string TemplateConfirmAccount = "d-b67630e1e9684b1f8dd6dd8dd33cfff8";

        public static string TemplateAdminNewUser = "d-84d8de39b7084b23b9ab2ce6146dfbc3";

        public const string PasswordResetUrl = "https://www.nightingaletrading.com/profile/passwordreset";
        public const string ConfirmAccountUrl = "https://www.nightingaletrading.com/api/account/confirm";
    }
}