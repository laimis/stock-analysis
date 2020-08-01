using System;
using System.Linq;
using core.Account;
using core.Adapters.Subscriptions;
using Xunit;

namespace coretests.Account
{
    public class UserTests
    {
        [Fact]
        public void CreatingSetsId()
        {
            var u = new User("laimis@gmail.com", "firstname", "last");

            Assert.NotEqual(Guid.Empty, u.State.Id);
        }

        [Theory]
        [InlineData(" ", "f", "l")]
        [InlineData("e", " ", "l")]
        [InlineData("e", "f", " ")]
        public void CreatingWithInvalidCombosFails(string email, string first, string last)
        {
            Assert.Throws<InvalidOperationException>(() => new User(email, first, last));
        }

        [Fact]
        public void SettingPasswordMatchEvalCorrect()
        {
            var u = new User("laimis@gmail.com", "firstname", "last");

            u.SetPassword("hash", "salt");

            Assert.True(u.PasswordHashMatches("hash"));
        }

        [Fact]
        public void Deleting_MarksAsDeleted()
        {
            var u = new User("laimis@gmail.com", "firstname", "last");

            u.Delete("delete feedback");

            Assert.NotNull(u.State.Deleted);
            Assert.Equal("delete feedback", u.State.DeleteFeedback);
        }

        [Fact]
        public void RequestPasswordReset()
        {
            var u = new User("laimis@gmail.com", "firstname", "last");

            u.RequestPasswordReset(DateTimeOffset.UtcNow);

            Assert.NotNull(u.Events.Single(e => e is core.Account.UserPasswordResetRequested));
        }

        [Fact]
        public void LastLoginTracked()
        {
            var u = new User("laimis@gmail.com", "firstname", "last");

            u.LoggedIn("blablabla", DateTimeOffset.UtcNow);

            Assert.Equal(u.State.LastLogin.Value.DateTime, DateTimeOffset.UtcNow.DateTime, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void Subscribe()
        {
            var u = new User("laimis@gmail.com", "firstname", "last");

            u.SubscribeToPlan(Plans.Full, "customer", "subscription");

            Assert.Equal(nameof(Plans.Full), u.State.SubscriptionLevel);
        }
    }
}