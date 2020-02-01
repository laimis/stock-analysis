namespace core.Account
{
    public class PasswordResetResult
    {
        private PasswordResetResult(string error)
        {
            this.Error = error;
        }

        public string Error { get; }
        
        internal static PasswordResetResult Failed(string error)
        {
            return new PasswordResetResult(error);
        }

        internal static PasswordResetResult Success()
        {
            return new PasswordResetResult(null);
        }
    }
}