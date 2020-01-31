using System;

namespace core.Account
{
    public class AuthenticateResult
    {
        private AuthenticateResult(string error)
        {
            this.Error = error;
        }

        private AuthenticateResult(User user)
        {
            this.User = user;
        }

        public string Error { get; }
        public User User { get; }

        internal static AuthenticateResult Failed(string error)
        {
            return new AuthenticateResult(error);
        }

        internal static AuthenticateResult Success(User user)
        {
            return new AuthenticateResult(user);
        }
    }
}