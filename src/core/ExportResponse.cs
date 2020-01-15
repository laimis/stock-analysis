namespace core
{
    public class ExportResponse
    {
        public ExportResponse(string filename, string content)
        {
            this.Filename = filename;
            this.Content = content;
        }

        public string Filename { get; }
        public string Content { get; }
        public string ContentType => "text/csv";
    }
}