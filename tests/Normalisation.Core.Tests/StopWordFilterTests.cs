using Normalisation.Core.Processors;

namespace Normalisation.Core.Tests
{
    [TestFixture]
    public class StopWordFilterTests
    {

        [SetUp]
        public void Setup() { }


        [TestCase("I will go to the market", "eng", "market")] //English
        [TestCase("Voy al mercado mañana", "spa", "Voy mercado mañana")] //Spanish
        [TestCase("Je vais au marché demain", "fra", "vais marché demain")] //French
        public void RemoveStopWords_FromInput_RemovesCorrectly(string input, string lang, string expected)
        {
            var normalisationSettings = new NormalisationSettings();

            var result = StopWordProcessor.RemoveStopWords(
                input, lang, normalisationSettings.LanguageDetectionFallbackIso2Code);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void RemoveStopWords_FromNoInput_ReturnsEmpty()
        {
            var result = StopWordProcessor.RemoveStopWords(
                "", "eng", "en");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void RemoveStopWords_UnknownLanguageCode_DefaultsToEnglishAndRemovesStopWords()
        {
            var input = "This is an unknown language";

            var result = StopWordProcessor.RemoveStopWords(
                input, "xx", "en"); //defaults to English

            Assert.That(result, Is.EqualTo("unknown language")); //stop words removed
        }
    }
}