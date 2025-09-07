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

            //add a folder to the url with training slash
            var musicBase = new Uri(_baseUrl.AbsoluteUri + "music");
            var musicUrls = new List<string>
            {
                "coldplay.html",   // relative
                "adel.html",       // relative
                "/top.html",       // root-relative
                "./local.html",    // current folder
                "../up.html"       // parent folder
            };

            var musicResults = UrlNormaliser.MakeAbsolute(musicUrls, musicBase);
            var musicResultStrings = musicResults.Select(u => u.ToString()).ToHashSet();

            Assert.That(musicResultStrings.Count, Is.EqualTo(5));
            Assert.That(musicResultStrings, Has.Some.EqualTo("https://example.com/music/coldplay.html"));
            Assert.That(musicResultStrings, Has.Some.EqualTo("https://example.com/music/adel.html"));
            Assert.That(musicResultStrings, Has.Some.EqualTo("https://example.com/top.html")); // root-relative
            Assert.That(musicResultStrings, Has.Some.EqualTo("https://example.com/music/local.html")); // ./ resolves to same folder
            Assert.That(musicResultStrings, Has.Some.EqualTo("https://example.com/up.html")); // ../ resolves to parent

            //add a folder to the url without traiing slash
            musicBase = new Uri(_baseUrl.AbsoluteUri + "music/");
            musicUrls = new List<string>
            {
                "coldplay.html",   // relative
                "adel.html",       // relative
                "/top.html",       // root-relative
                "./local.html",
                "../up.html"
            };

            musicResults = UrlNormaliser.MakeAbsolute(musicUrls, musicBase);
            musicResultStrings = musicResults.Select(u => u.ToString()).ToHashSet();

            Assert.That(musicResultStrings.Count, Is.EqualTo(5));
            Assert.That(musicResultStrings, Has.Some.EqualTo("https://example.com/music/coldplay.html"));
            Assert.That(musicResultStrings, Has.Some.EqualTo("https://example.com/music/adel.html"));
            Assert.That(musicResultStrings, Has.Some.EqualTo("https://example.com/top.html")); // root-relative
            Assert.That(musicResultStrings, Has.Some.EqualTo("https://example.com/music/local.html"));
            Assert.That(musicResultStrings, Has.Some.EqualTo("https://example.com/up.html"));
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
