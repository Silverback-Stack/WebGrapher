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
            var expiresActual = DateTimeOffset.UtcNow.AddYears(1);
            var expiresActualAsTimeSpan = expiresActual - DateTimeOffset.UtcNow;

            var result = CacheDurationHelper.Clamp(expiresActual, maxDurationInMinutes: 1);

            Assert.That(result, Is.LessThan(expiresActualAsTimeSpan));
        }
    }
}
