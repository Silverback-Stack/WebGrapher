using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core.Helpers;

namespace Shared.Caching.Tests
{
    [TestFixture]
    internal class CacheDurationHelperTests
    {
        [Test]
        public void Clamp_ExpiryFarInFuture_ReturnsClampedDuration()
        {
            var expiresActual = DateTimeOffset.UtcNow.AddMinutes(60);
            var expiresActualAsTimeSpan = expiresActual - DateTimeOffset.UtcNow;

            var minDirationInMinutes = 10;
            var maxDirationInMinutes = 20;
            var result = CacheDurationHelper.Clamp(expiresActual, minDirationInMinutes, maxDirationInMinutes);

            Assert.That(result, Is
                .GreaterThanOrEqualTo(TimeSpan.FromMinutes(minDirationInMinutes))
                .And.LessThanOrEqualTo(TimeSpan.FromMinutes(maxDirationInMinutes)));
        }

        [Test]
        public void Clamp_ExpiryInPast_ReturnsNull()
        {
            var expiresActual = DateTimeOffset.UtcNow.AddMinutes(-10);
            var expiresActualAsTimeSpan = expiresActual - DateTimeOffset.UtcNow;

            var minDirationInMinutes = 10;
            var maxDirationInMinutes = 20;
            var result = CacheDurationHelper.Clamp(expiresActual, minDirationInMinutes, maxDirationInMinutes);

            Assert.That(result, Is.Null);
        }
    }
}
