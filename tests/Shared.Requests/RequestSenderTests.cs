using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Requests.Core;

namespace Shared.Requests
{
    [TestFixture]
    internal class RequestSenderTests
    {
        [Test]
        public void BuildCacheKey_LongInputLenghts_ReturnsHash()
        {
            var url = new Uri("https://www.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx.com");
            var userAgent = "UserAgent_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
            var acceptHeader = "Accept_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

            var result = RequestSender.BuildCacheKey(url, userAgent, acceptHeader);

            //SHA-256 hash output → 256 bits = 32 bytes
            Assert.That(result, Has.Length.LessThan(50)); 
        }

        [Test]
        public void GetCacheDuration_ExpiryTooFarInFuture_ReturnsClampedCacheDuration()
        {
            var expiresActual = DateTimeOffset.UtcNow.AddYears(1);
            var expiresActualTimeSpan = expiresActual - DateTimeOffset.UtcNow;

            var result = RequestSender.GetCacheDuration(expiresActual, expiryMinutes: 5);

            Assert.That(result, Is.LessThan(expiresActualTimeSpan));
        }
    }
}
