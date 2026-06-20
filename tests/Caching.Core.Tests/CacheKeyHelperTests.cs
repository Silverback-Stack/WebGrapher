using Caching.Core.Helpers;

namespace Caching.Core.Tests
{
    [TestFixture]
    public class CacheKeyHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public void Generate_LongInputLengths_ReturnSmallerLengthHash()
        {
            var url = new Uri("https://www.example.com");
            var userAgent = "UserAgent_XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
            var userAccepts = "Accepts_XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

            var compositeKey = $"{url.AbsoluteUri}|{userAgent}|{userAccepts}";
            var result = CacheKeyHelper.ComputeCacheKey(compositeKey);

            // SHA-256 produces 32 bytes (256 bits),
            // represented as a 64-character hexadecimal string.
            Assert.That(result, Has.Length.EqualTo(64));
        }


        [Test]
        public void Generate_ConsistentValueGenerated_ReturnTrue()
        {
            var url = new Uri("https://www.example.com");
            var userAgent = "UserAgent_Example";
            var userAccepts = "Accepts_Example";

            var compositeKey = $"{url.AbsoluteUri}|{userAgent}|{userAccepts}";

            var result1 = CacheKeyHelper.ComputeCacheKey(compositeKey);

            var result2 = CacheKeyHelper.ComputeCacheKey(compositeKey);

            Assert.That(result1, Is.EqualTo(result2));
        }
    }
}