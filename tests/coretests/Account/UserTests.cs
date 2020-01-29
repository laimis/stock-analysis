using System;
using core.Account;
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
    }
}