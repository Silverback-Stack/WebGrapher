using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Normalisation.Core.Processors;

namespace Service.Normalisation.Tests
{
    [TestFixture]
    public class UrlNormaliserTests
    {
        private Uri _baseUrl;

        [SetUp]
        public void Setup()
        {
            _baseUrl = new Uri("https://example.com/");
        }

        [Test]
        public void MakeAbsolute_ConvertsRelativeUrls_ReturnsAbsoluteUrls()
        {
            var urls = new List<string>
            {
                "/page1",                     // root-relative
                "page2",                      // relative
                "https://other.com/page3",    // absolute
                "//other.com/page4",          // protocol-relative with host
                "//images/page5",             // protocol-relative without host
                "page6#fragment",             // relative with fragment
                "",                           // empty
                " "                           // whitespace
            };

            var results = UrlNormaliser.MakeAbsolute(urls, _baseUrl);

            // Remove fragments for comparison
            var resultStrings = results.Select(u => u.ToString()).ToHashSet();

            Assert.That(resultStrings.Count, Is.EqualTo(6));

            Assert.That(resultStrings, Has.Some.EqualTo("https://example.com/page1"));
            Assert.That(resultStrings, Has.Some.EqualTo("https://example.com/page2"));
            Assert.That(resultStrings, Has.Some.EqualTo("https://other.com/page3"));
            Assert.That(resultStrings, Has.Some.EqualTo("https://other.com/page4"));
            Assert.That(resultStrings, Has.Some.EqualTo("https://example.com/images/page5"));
            Assert.That(resultStrings, Has.Some.EqualTo("https://example.com/page6"));
        }

        [Test]
        public void FilterBySchema_FiltersUrlsByScheme()
        {
            var urls = new HashSet<Uri>
            {
                new Uri("https://example.com"),
                new Uri("http://example.com"),
                new Uri("ftp://example.com"),
                new Uri("mailto:email@example.com"),
                new Uri("tel:0123456789"),
            };

            var filtered = UrlNormaliser.FilterBySchema(urls, new[] { "https", "ftp" });

            Assert.That(filtered.Count, Is.EqualTo(2));
            Assert.That(filtered.Any(u => u.Scheme == "https"), Is.True);
            Assert.That(filtered.Any(u => u.Scheme == "ftp"), Is.True);
        }

        [Test]
        public void FilterByRegex_FiltersUrlsContainingMatchingPattern()
        {
            var urls = new HashSet<Uri>
        {
            new Uri("https://example.com/products/item1"),
            new Uri("https://example.com/blog/post"),
            new Uri("https://example.com/contact"),
            new Uri("https://example.com/chat/hello")
        };

            //match only URLs that contain either /products/ or /blog/
            var filtered = UrlNormaliser.FilterByRegex(urls, ".*/(products|blog)/.*"); 


            Assert.That(filtered.Count, Is.EqualTo(2));
            Assert.That(filtered.Any(u => u.AbsolutePath.Contains("products")), Is.True);
            Assert.That(filtered.Any(u => u.AbsolutePath.Contains("blog")), Is.True);
        }

        [Test]
        public void RemoveExternalLinks_RemovesUrlsOutsideBaseAuthority()
        {
            var urls = new HashSet<Uri>
        {
            new Uri("https://example.com/page1"),
            new Uri("https://external.com/page2")
        };

            var filtered = UrlNormaliser.RemoveExternalLinks(urls, _baseUrl);

            Assert.That(filtered.Count, Is.EqualTo(1));
            Assert.That(filtered.First().Host, Is.EqualTo("example.com"));
        }

        [Test]
        public void RemoveQueryStrings_RemovesQueryFromUrls()
        {
            var urls = new HashSet<Uri>
        {
            new Uri("https://example.com/page?query=1"),
            new Uri("https://example.com/page2?search=test")
        };

            var cleaned = UrlNormaliser.RemoveQueryStrings(urls);

            Assert.That(cleaned.All(u => string.IsNullOrEmpty(u.Query)), Is.True);
        }

        [Test]
        public void RemoveCyclicalLinks_RemovesBaseUrlFromSet()
        {
            var urls = new HashSet<Uri>
        {
            _baseUrl,
            new Uri("https://example.com/other")
        };

            var filtered = UrlNormaliser.RemoveCyclicalLinks(urls, _baseUrl);

            Assert.That(filtered.Contains(_baseUrl), Is.False);
            Assert.That(filtered.Count, Is.EqualTo(1));
        }


        //DO NOT USE - CAUSING UNEXPECTED REDIRECTS - ALWAYS STAY TRUE TO SOURCE PAGE URLS
        //[Test]
        //public void RemoveTrailingSlash_RemovesTrailingSlashExceptRoot()
        //{
        //    var urls = new HashSet<Uri>
        //{
        //    new Uri("https://example.com/path/"),
        //    new Uri("https://example.com/"),
        //    new Uri("https://example.com/path/sub/")
        //};

        //    var cleaned = UrlNormaliser.RemoveTrailingSlash(urls);

        //    Assert.That(cleaned.Any(u => u.ToString().EndsWith("/path")), Is.True);
        //    Assert.That(cleaned.Any(u => u.ToString().EndsWith("/")), Is.True); // root remains with slash
        //    Assert.That(cleaned.Any(u => u.ToString().EndsWith("/sub")), Is.True);
        //}

        [Test]
        public void Truncate_ReturnsOnlySpecifiedNumberOfUrls()
        {
            var urls = new HashSet<Uri>
        {
            new Uri("https://example.com/1"),
            new Uri("https://example.com/2"),
            new Uri("https://example.com/3")
        };

            var truncated = UrlNormaliser.Truncate(urls, 2);

            Assert.That(truncated.Count, Is.EqualTo(2));
        }

    }
}
