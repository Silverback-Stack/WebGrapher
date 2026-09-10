using Normalisation.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Normalisation.Core.Tests
{
    [TestFixture]
    public class FingerprintHelperTests
    {
        private PageDataStandardised? _pageDataStandardised;


        [SetUp]
        public void Setup()
        {
            _pageDataStandardised = new PageDataStandardised
            {
                Title = "Example Page",
                Summary = "An example page summary.",
                Keywords = "example page keywords",
                Tags = ["example", "page", "test"],
                Links =
                    [
                        new Uri("https://www.example.com/page-1"),
                        new Uri("https://www.example.com/page-2")
                    ],
                ImageUrl = new Uri("https://www.example.com/image.jpg"),
                ImageCors = true,
                LanguageIso3 = "eng"
            };
        }


        [Test]
        public void ComputeFingerprint_ReturnsSha256Hash()
        {
            var result = FingerprintHelper.ComputeFingerprint(_pageDataStandardised);

            // SHA-256 produces 32 bytes (256 bits),
            // represented as a 64-character hexadecimal string.
            Assert.That(result, Has.Length.EqualTo(64));
        }


        [Test]
        public void ComputeFingerprint_ConsistentValueGenerated_ReturnTrue()
        {
            var result1 = FingerprintHelper.ComputeFingerprint(_pageDataStandardised);
            var result2 = FingerprintHelper.ComputeFingerprint(_pageDataStandardised);

            Assert.That(result1, Is.EqualTo(result2));
        }


        [Test]
        public void ComputeFingerprint_DifferentCollectionOrder_ReturnsSameFingerprint()
        {
            // Generate fingerprint with original order
            var result1 = FingerprintHelper.ComputeFingerprint(_pageDataStandardised);

            // Change order of collections
            _pageDataStandardised.Tags =
                _pageDataStandardised.Tags?.Reverse().ToArray();
            _pageDataStandardised.Links =
                _pageDataStandardised.Links?.Reverse().ToArray();

            // Generate fingerprint with reordered collections
            var result2 = FingerprintHelper.ComputeFingerprint(_pageDataStandardised);

            // Make sure they still match
            Assert.That(result1, Is.EqualTo(result2));
        }
    }
}
