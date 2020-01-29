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
            var u = new User("laimis@gmail.com");

            Assert.NotEqual(Guid.Empty, u.State.Id);
        }

        [Fact]
        public void CreatingWithNoEmailThrows()
        {
            Assert.Throws<InvalidOperationException>(() => new User(" "));
        }
    }
}