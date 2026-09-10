using Normalisation.Core.Processors;

namespace Normalisation.Core.Tests
{
    [TestFixture]
    public class HtmlProcessorTests
    {
        private HtmlProcessor? _htmlProcessor;

        [SetUp]
        public void Setup()
        {
            var htmlDocument = @"
            <html>
                <head>
                    <title>Title</title>
                </head>
                <body>
                    <nav>
                        Navigation
                    </nav>

                    <main>
                        <h1>Heading text</h1>

                        <p class='summary'>
                            Summary text
                        </p>

                        <div class='content'>
                            <p>
                                Content text
                            </p>

                            <a href='http://www.externalsite.com'>External site link</a>

                            <div class='logo'>
                                <img src='/images/logo.jpg' />
                            </div>
                        </div>

                        <section id='related-links'>
                            <h2>Related Links</h2>
                            <a href='relative-link.html'>Relative link</a>
                            <a href='http://www.example.com/absolute-link.html'>Absolute link</a>
                        </section>
                    </main>

                    <footer>
                        Footer
                    </footer>
                </body>
            </html>";

            _htmlProcessor = new HtmlProcessor(htmlDocument);
        }

        [Test]
        public void ExtractTitle_WithoutXPath_ReturnsDocumentTitle()
        {
            var title = _htmlProcessor?.ExtractTitle();

            Assert.That(title, Is.EqualTo("Title"));
        }


        [Test]
        public void ExtractTitle_WithXPath_ReturnsIdentifiedTitle()
        {
            var title = _htmlProcessor?.ExtractTitle("//main//h1");

            Assert.That(title, Is.EqualTo("Heading text"));
        }


        [Test]
        public void ExtractContentAsPlainText_WithoutXPath_ReturnsDocumentContent()
        {
            var content = _htmlProcessor?.ExtractContentAsPlainText();

            Assert.That(content, Does.Contain("Navigation"));
            Assert.That(content, Does.Contain("Heading text"));
            Assert.That(content, Does.Contain("Summary text"));
            Assert.That(content, Does.Contain("Content text"));
            Assert.That(content, Does.Contain("External site link"));
            Assert.That(content, Does.Contain("Related Links"));
            Assert.That(content, Does.Contain("Relative link"));
            Assert.That(content, Does.Contain("Absolute link"));
            Assert.That(content, Does.Contain("Footer"));
        }


        [Test]
        public void ExtractContentAsPlainText_WithXPath_ReturnsIdentifiedContent()
        {
            var content = _htmlProcessor?.ExtractContentAsPlainText(
                "//div[@class='content']",
                "Content");

            Assert.That(content, Does.Contain("Content text"));
            Assert.That(content, Does.Contain("External site link"));
        }


        [Test]
        public void ExtractLinkReferences_WithoutXPath_ReturnsDocumentLinks()
        {
            var links = _htmlProcessor?.ExtractLinkReferences().ToList();

            Assert.That(links, Has.Count.EqualTo(3));
            Assert.That(links, Does.Contain("http://www.externalsite.com"));
            Assert.That(links, Does.Contain("relative-link.html"));
            Assert.That(links, Does.Contain("http://www.example.com/absolute-link.html"));
        }


        [Test]
        public void ExtractLinkReferences_WithXPath_ReturnsIdentifiedLinks()
        {
            var links = _htmlProcessor?
                .ExtractLinkReferences("//section[@id='related-links']")
                .ToList();

            Assert.That(links, Has.Count.EqualTo(2));
            Assert.That(links, Does.Contain("relative-link.html"));
            Assert.That(links, Does.Contain("http://www.example.com/absolute-link.html"));

            Assert.That(links, Does.Not.Contain("http://www.externalsite.com"));
        }


        [Test]
        public void ExtractImageReference_WithXPath_ReturnsIdentifiedImage()
        {
            var image = _htmlProcessor?.ExtractImageReference(
                "//div[@class='logo']//img");

            Assert.That(image, Is.EqualTo("/images/logo.jpg"));
        }


        [Test]
        public void ExtractContentAsPlainText_WithXPathNotFound_ReturnsEmptyString()
        {
            var content = _htmlProcessor?.ExtractContentAsPlainText(
                "//div[@id='not-found']",
                "Content");

            Assert.That(content, Is.Empty);
        }
    }
}