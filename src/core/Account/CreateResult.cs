using System;

namespace core.Account
{
    public class CreateResult
    {
        private CreateResult(string error)
        {
            this.Error = error;
        }

        private CreateResult(User user)
        {
            this.User = user;
        }

        public string Error { get; }
        public User User { get; }

        internal static CreateResult Failed(string error)
        {
            return new CreateResult(error);
        }

        internal static CreateResult Success(User user)
        {
            return new CreateResult(user);
        }
    }
}