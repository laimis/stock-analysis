namespace core.Shared.Adapters.CSV
{
    public class ExportResponse
    {
        public ExportResponse(string filename, string content)
        {
            Filename = filename;
            Content = content;
        }

        public string Filename { get; }
        public string Content { get; }
        public string ContentType => "text/csv";
    }
}