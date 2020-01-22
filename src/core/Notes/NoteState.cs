using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Notes
{
    public class NoteState
    {
        public string Id { get; internal set; }
        public string RelatedToTicker { get; internal set; }
        public DateTime Created { get; internal set; }
        public string Note { get; internal set; }
        public double? PredictedPrice { get; internal set; }
        public string UserId { get; internal set; }
        public bool IsArchived { get; internal set; }
        public DateTimeOffset? ReminderDate { get; internal set; }
        public bool HasReminder => ReminderDate.HasValue;
        public IEnumerable<object> Followups => _followups.Select(f => new {
            date = f.Key,
            text = f.Value
        });

        private List<KeyValuePair<DateTime, string>> _followups = new List<KeyValuePair<DateTime, string>>();
        internal void AddFollowup(DateTime when, string text)
        {
            _followups.Add(new KeyValuePair<DateTime, string>(when, text));
        }
    }
}