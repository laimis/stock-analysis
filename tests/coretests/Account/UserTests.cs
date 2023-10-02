using System;
using System.Linq;
using System.Runtime.Serialization;
using core.fs.Shared.Adapters.Subscriptions;
using core.fs.Shared.Domain.Accounts;
using Xunit;

namespace coretests.Account
{
    public class UserTests
    {
        [Fact]
        public void CreatingSetsId()
        {
            var u = User.Create("laimis@gmail.com", "firstname", "last");

            Assert.NotEqual(Guid.Empty, u.State.Id);
        }

        [Theory]
        [InlineData(" ", "f", "l")]
        [InlineData("e", " ", "l")]
        [InlineData("e", "f", " ")]
        public void CreatingWithInvalidCombosFails(string email, string first, string last)
        {
            Assert.Throws<ArgumentException>(() => User.Create(email, first, last));
        }

        [Fact]
        public void SettingPasswordMatchEvalCorrect()
        {
            var u = User.Create("laimis@gmail.com", "firstname", "last");

            u.SetPassword("hash", "salt");

            Assert.True(u.PasswordHashMatches("hash"));
        }

        [Fact]
        public void Deleting_MarksAsDeleted()
        {
            var u = User.Create("laimis@gmail.com", "firstname", "last");

            u.Delete("delete feedback");

            Assert.NotNull(u.State.Deleted);
            Assert.Equal("delete feedback", u.State.DeleteFeedback);
        }

        [Fact]
        public void RequestPasswordReset()
        {
            var u = User.Create("laimis@gmail.com", "firstname", "last");

            u.RequestPasswordReset(DateTimeOffset.UtcNow);

            Assert.NotNull(u.Events.Single(e => e is core.Account.UserPasswordResetRequested));
        }

        [Fact]
        public void Subscribe()
        {
            var u = User.Create("laimis@gmail.com", "firstname", "last");

            u.SubscribeToPlan(Plans.Full, "customer", "subscription");
            // TODO: bring it back once fsharp conversion is done
            // Assert.Equal(nameof(Plans.Full), u.State.SubscriptionLevel);
        }
    }
}