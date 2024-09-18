using System;
using System.Linq;
using core.fs.Accounts;
using core.fs.Adapters.Subscriptions;
using testutils;
using Xunit;

namespace coretests.Account
{
    public class UserTests
    {
        [Fact]
        public void CreatingSetsId()
        {
            var u = User.Create(TestDataGenerator.RandomEmail(), "firstname", "last");

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
            var u = User.Create(TestDataGenerator.RandomEmail(), "firstname", "last");

            u.SetPassword("hash", "salt");

            Assert.True(u.PasswordHashMatches("hash"));
        }

        [Fact]
        public void Deleting_MarksAsDeleted()
        {
            var u = User.Create(TestDataGenerator.RandomEmail(), "firstname", "last");

            u.Delete("delete feedback");

            Assert.NotNull(u.State.Deleted);
            Assert.Equal("delete feedback", u.State.DeleteFeedback);
        }

        [Fact]
        public void RequestPasswordReset()
        {
            var u = User.Create(TestDataGenerator.RandomEmail(), "firstname", "last");

            u.RequestPasswordReset(DateTimeOffset.UtcNow);

            Assert.NotNull(u.Events.Single(e => e is core.Account.UserPasswordResetRequested));
        }

        [Fact]
        public void Subscribe()
        {
            var u = User.Create(TestDataGenerator.RandomEmail(), "firstname", "last");

            u.SubscribeToPlan(Plans.Full, "customer", "subscription");
            // TODO: bring it back once fsharp conversion is done
            // Assert.Equal(nameof(Plans.Full), u.State.SubscriptionLevel);
        }

        [Fact]
        public void SetSetting()
        {
            var u = User.Create(TestDataGenerator.RandomEmail(), "firstname", "last");

            u.SetSetting("maxLoss", "60");
            
            Assert.Equal(60m, u.State.MaxLoss);
        }
        
        [Fact]
        public void SetSetting_InvalidKey()
        {
            var u = User.Create(TestDataGenerator.RandomEmail(), "firstname", "last");

            Assert.Throws<InvalidOperationException>(() => u.SetSetting("maxloss", "60.1"));
        }
        
        [Fact]
        public void SetSetting_InvalidValue()
        {
            var u = User.Create(TestDataGenerator.RandomEmail(), "firstname", "last");

            Assert.Throws<FormatException>(() => u.SetSetting("maxLoss", "large"));
        }
        
        [Fact]
        public void ConnectToBrokerage_SetsState()
        {
            var u = User.Create(TestDataGenerator.RandomEmail(), "firstname", "last");

            u.ConnectToBrokerage("access", "refresh", "token", 3600, "Accounts");

            Assert.True(u.State.ConnectedToBrokerage);
            Assert.Equal("access", u.State.BrokerageAccessToken);
            Assert.Equal("refresh", u.State.BrokerageRefreshToken);
            Assert.Equal(DateTimeOffset.UtcNow.AddSeconds(1800), u.State.BrokerageAccessTokenExpires, TimeSpan.FromSeconds(1));
            Assert.Equal(DateTimeOffset.UtcNow.AddDays(7), u.State.BrokerageRefreshTokenExpires,
                TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void RefreshBrokerageConnection_SetsState()
        {
            var u = User.Create(TestDataGenerator.RandomEmail(), "firstname", "last");
            
            u.RefreshBrokerageConnection("access", "refresh", "token", 3600, "Accounts");
            
            Assert.True(u.State.ConnectedToBrokerage);
            Assert.Equal("access", u.State.BrokerageAccessToken);
            Assert.Equal("refresh", u.State.BrokerageRefreshToken);
            Assert.Equal(DateTimeOffset.UtcNow.AddSeconds(1800), u.State.BrokerageAccessTokenExpires, TimeSpan.FromSeconds(1));
            Assert.Equal(DateTimeOffset.UtcNow.AddDays(7), u.State.BrokerageRefreshTokenExpires,
                TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void InterestReceived_Works()
        {
            var u = User.Create(TestDataGenerator.RandomEmail(), "firstname", "last");
            
            u.ApplyBrokerageInterest(DateTimeOffset.UtcNow, "interest1", 13.2m);
            u.ApplyBrokerageInterest(DateTimeOffset.UtcNow, "interest2", 13.2m);
            // reapplying again, ignores it
            u.ApplyBrokerageInterest(DateTimeOffset.UtcNow, "interest1", 13.2m);
            
            Assert.Equal(26.4m, u.State.InterestReceived);
        }
    }
}
