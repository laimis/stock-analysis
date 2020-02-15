namespace core.Adapters.Emails
{
    public static class EmailSettings
    {
        public const string Admin = "laimis@gmail.com";

        public const string TemplatePasswordReset = "d-6f4ac095859a417d88e8243d8056838b";
        public const string TemplateConfirmAccount = "d-b67630e1e9684b1f8dd6dd8dd33cfff8";
        public const string TemplateReviewEmail = "d-b03188c3d1d74945adb7732b3684ef4b";

        public const string TemplateAdminNewUser = "d-84d8de39b7084b23b9ab2ce6146dfbc3";
        public const string TemplateAdminUserDeleted = "d-3bdfe7613d1048e5b1a469a155f945b0";
        public const string TemplateAdminContact = "d-ab673f8ba0be40018dcc20aca9880d63";

        public const string PasswordResetUrl = "https://www.nightingaletrading.com/profile/passwordreset";
        public const string ConfirmAccountUrl = "https://www.nightingaletrading.com/api/account/confirm";
    }
}