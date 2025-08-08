using Normalisation.Core.Processors;

namespace Service.Normalisation.Tests
{
    [TestFixture]
    public class HtmlParserTests
    {
        private HtmlParser? _htmlParser;

        [SetUp]
        public void Setup()
        {
            var htmlDocument = @"
            <html>
                <head>
                    <title>Title</title>
                </head>
                <body>
                    <h1>Heading1</h1>
                    <p>Paragraph</p>
                    <a href='http://www.example.com'>Link</a>
                </body>
            </html>";

            _htmlParser = new HtmlParser(htmlDocument);
        }

        [Test]
        public void ExtractTitle_FromHtmlDocument_ReturnsTitle()
        {
            var title = _htmlParser?.ExtractTitle();

            Assert.That(title, Is.EqualTo("Title"));
        }

        [Test]
        public void ExtractContentAsPlainText_FromHtmlDocument_ReturnsContent()
        {
            var content = _htmlParser?.ExtractContentAsPlainText();

            Assert.That(content, Does.Contain("Heading1"));
            Assert.That(content, Does.Contain("Paragraph"));
            Assert.That(content, Does.Contain("Link"));
        }

        [Test]
        public void ExtractLinks_FromHtmlDocument_ReturnsLinks()
        {
            var links = _htmlParser?.ExtractLinks().ToList();

            Assert.That(links, Has.Count.EqualTo(1));
            Assert.That(links, Does.Contain("http://www.example.com"));
        }
    }
}