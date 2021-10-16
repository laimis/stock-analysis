using System.Collections.Generic;
using System.Linq;

namespace core.Notes.Output
{
    public class NotesList
    {
        public NotesList(IEnumerable<NoteState> notes)
        {
            Notes = notes;
            Tickers = notes
                .Select(n => n.RelatedToTicker)
                .Distinct()
                .OrderBy(s => s);
        }

        public IEnumerable<NoteState> Notes { get; }
        public IEnumerable<string> Tickers { get; }
    }
}