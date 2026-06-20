using System;
using Caching.Core.Helpers;

namespace Caching.Core.Tests
{
    [TestFixture]
    internal class CacheDurationHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public void Clamp_ExpiryFarInFuture_ReturnsClampedDuration()
        {
            var expiresActual = DateTimeOffset.UtcNow.AddMinutes(60);

            var minDurationInMinutes = 10;
            var maxDurationInMinutes = 20;
            var result = CacheDurationHelper.Clamp(expiresActual, minDurationInMinutes, maxDurationInMinutes);

            Assert.That(result, Is
                .GreaterThanOrEqualTo(TimeSpan.FromMinutes(minDurationInMinutes))
                .And.LessThanOrEqualTo(TimeSpan.FromMinutes(maxDurationInMinutes)));
        }


        [Test]
        public void Clamp_ExpiryInPast_ReturnsMinTTL()
        {
            var expiresActual = DateTimeOffset.UtcNow.AddMinutes(-10);

            var minDurationInMinutes = 10;
            var maxDurationInMinutes = 20;

            var result = CacheDurationHelper.Clamp(expiresActual, minDurationInMinutes, maxDurationInMinutes);

            Assert.That(result, Is.EqualTo(TimeSpan.FromMinutes(minDurationInMinutes)));
        }

        [Test]
        public void Clamp_ExpiryThatIsNull_ReturnsMinTTL()
        {
            DateTimeOffset? expiresActual = null;

            var minDurationInMinutes = 10;
            var maxDurationInMinutes = 20;

            var result = CacheDurationHelper.Clamp(expiresActual, minDurationInMinutes, maxDurationInMinutes);

            Assert.That(result, Is.EqualTo(TimeSpan.FromMinutes(minDurationInMinutes)));
        }
    }
}
