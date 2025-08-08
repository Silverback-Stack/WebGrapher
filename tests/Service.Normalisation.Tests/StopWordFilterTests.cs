using Normalisation.Core.Processors;

namespace Service.Normalisation.Tests
{
    [TestFixture]
    public class StopWordFilterTests
    {

        [SetUp]
        public void Setup()
        {

        }


        [TestCase("I will go to the market", "en", "market")] //English
        [TestCase("Voy al mercado ma�ana", "es", "Voy mercado ma�ana")] //Spanish
        [TestCase("Je vais au march� demain", "fr", "vais march� demain")] //French
        public void RemoveStopWords_FromInput_RemovesCorrectly(string input, string lang, string expected)
        {
            var result = StopWordFilter.RemoveStopWords(input, lang);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void RemoveStopWords_FromInput_ReturnsEmpty()
        {
            var result = StopWordFilter.RemoveStopWords("", "en");
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void RemoveStopWords_UnknownLanguageCode_ReturnsInputUnchanged()
        {
            var input = "This is a test";
            var result = StopWordFilter.RemoveStopWords(input, "xx");
            Assert.That(result, Is.EqualTo(input));
        }
    }
}