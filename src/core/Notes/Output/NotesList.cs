using System.Collections.Generic;

namespace core.Notes.Output
{
    public class NotesList
    {
        public IEnumerable<NoteState> Notes { get; internal set; }
    }
}