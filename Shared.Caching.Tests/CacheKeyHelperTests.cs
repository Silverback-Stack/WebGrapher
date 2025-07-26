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

            var result = CacheKeyHelper.Generate(url, userAgent, userAccepts);

            //SHA-256 hash output → 256 bits = 32 bytes
            Assert.That(result, Has.Length.LessThanOrEqualTo(64));
        }
    }
}