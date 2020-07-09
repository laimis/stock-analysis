using System;
using System.Collections.Generic;
using System.Linq;
using core.Account;
using core.Alerts;
using core.Notes;
using core.Options;
using core.Stocks;

namespace core.Admin
{
    internal class UserView
    {
        public UserView(User user, IEnumerable<OwnedStock> stocks, IEnumerable<OwnedOption> options, IEnumerable<Note> notes, IEnumerable<Alert> alerts)
        {
            Email = user.Email;
            UserId = user.Id;
            Firstname = user.Firstname;
            Lastname = user.Lastname;
            LastLogin = user.LastLogin;
            Verified = user.Verified;
            Stock = stocks.Count();
            Options = options.Count();
            Notes = notes.Count();
            Alerts = alerts.Count();
        }

        public string Email { get; }
        public Guid UserId { get; }
        public string Firstname { get; }
        public string Lastname { get; }
        public DateTimeOffset? LastLogin { get; }
        public bool Verified { get; }
        public int Stock { get; }
        public int Options { get; }
        public int Notes { get; }
        public int Alerts { get; }
    }
}