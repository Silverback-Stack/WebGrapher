using Normalisation.Core.Processors;

namespace Service.Normalisation.Tests
{
    [TestFixture]
    public class StopWordFilterTests
    {

        [SetUp]
        public void Setup() { }


        [TestCase("I will go to the market", "en", "market")] //English
        [TestCase("Voy al mercado ma�ana", "es", "Voy mercado ma�ana")] //Spanish
        [TestCase("Je vais au march� demain", "fr", "vais march� demain")] //French
        public void RemoveStopWords_FromInput_RemovesCorrectly(string input, string lang, string expected)
        {
            var result = StopWordFilter.RemoveStopWords(input, lang);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void RemoveStopWords_FromNoInput_ReturnsEmpty()
        {
            var result = StopWordFilter.RemoveStopWords("", "en");
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void RemoveStopWords_UnknownLanguageCode_DefaultsToEnglishAndRemovesStopWords()
        {
            var input = "This is a test of unknown language";
            var result = StopWordFilter.RemoveStopWords(input, "xx"); //defaults to English
            Assert.That(result, Is.EqualTo("unknown language")); //stop words removed
        }
    }
}