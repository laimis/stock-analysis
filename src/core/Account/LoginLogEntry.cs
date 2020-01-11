using System;

namespace core.Account
{
    public class LoginLogEntry
    {
        public LoginLogEntry(string username, DateTime date)
        {
            this.Username = username;
            this.Date = date;
        }

        public string Username { get; }
        public DateTime Date { get; }
    }
}