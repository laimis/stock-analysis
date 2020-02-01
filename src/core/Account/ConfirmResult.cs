namespace core.Account
{
    public class ConfirmResult
    {
        private ConfirmResult(string error)
        {
            this.Error = error;
        }

        private ConfirmResult(User user)
        {
            this.User = user;
        }

        public string Error { get; }
        public User User { get; }

        internal static ConfirmResult Failed(string error)
        {
            return new ConfirmResult(error);
        }

        internal static ConfirmResult Success(User user)
        {
            return new ConfirmResult(user);
        }
    }
}