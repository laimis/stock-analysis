using System;
using System.Collections.Generic;
using System.Linq;
using core.Account;
using core.Notes;
using core.Options;
using core.Stocks;

namespace core.Admin
{
    internal class UserView
    {
        public UserView(User user, IEnumerable<OwnedStock> stocks, IEnumerable<OwnedOption> options, IEnumerable<Note> notes)
        {
            Email = user.State.Email;
            UserId = user.Id;
            Firstname = user.State.Firstname;
            Lastname = user.State.Lastname;
            Verified = user.State.Verified != null;
            Stock = stocks.Count();
            Options = options.Count();
            Notes = notes.Count();
        }

        public string Email { get; }
        public Guid UserId { get; }
        public string Firstname { get; }
        public string Lastname { get; }
        public bool Verified { get; }
        public int Stock { get; }
        public int Options { get; }
        public int Notes { get; }
    }
}