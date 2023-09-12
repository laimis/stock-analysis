using core;
using core.Shared;
using core.Shared.Adapters.CSV;
using Xunit;

namespace coretests
{
    public class ExportResponseTests
    {
        private readonly ExportResponse _response = new("filename", "content");

        [Fact]
        public void CorrectContentType()
        {
            Assert.Equal("text/csv", _response.ContentType);
        }

        [Fact]
        public void CorrectFilename()
        {
            Assert.Equal("filename", _response.Filename);
        }

        [Fact]
        public void CorrectContent()
        {
            Assert.Equal("content", _response.Content);
        }
    }
}