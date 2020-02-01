namespace core.Account
{
    public class ResetPasswordResult
    {
        private ResetPasswordResult(string error)
        {
            this.Error = error;
        }

        private ResetPasswordResult(User user)
        {
            this.User = user;
        }

        public string Error { get; }
        public User User { get; }

        internal static ResetPasswordResult Failed(string error)
        {
            return new ResetPasswordResult(error);
        }

        internal static ResetPasswordResult Success(User user)
        {
            return new ResetPasswordResult(user);
        }
    }
}