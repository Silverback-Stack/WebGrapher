using Caching.Core.Helpers;

namespace Shared.Caching.Tests
{
    [TestFixture]
    public class CacheKeyHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Generate_LongInputLenghts_ReturnSmallerLengthHash()
        {
            var url = new Uri("https://www.example.com");
            var userAgent = "UserAgent_XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
            var userAccepts = "Accepts_XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

            var result = CacheKeyHelper.Generate(url.AbsoluteUri, userAgent, userAccepts);

            //SHA-256 hash output → 256 bits = 32 bytes
            Assert.That(result, Has.Length.LessThanOrEqualTo(64));
        }

        [Test]
        public void Generate_ConsistentValueGenerated_ReturnTrue()
        {
            var url = new Uri("https://www.example.com");
            var userAgent = "UserAgent_Example";
            var userAccepts = "Accepts_Example";

            var result1 = CacheKeyHelper.Generate(url.AbsoluteUri, userAgent, userAccepts);

            var result2 = CacheKeyHelper.Generate(url.AbsoluteUri, userAgent, userAccepts);

            Assert.That(result1, Is.EquivalentTo(result2));
        }
    }
}