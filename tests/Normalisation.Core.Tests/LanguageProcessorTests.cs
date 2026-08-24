using Normalisation.Core.Processors;

namespace Normalisation.Core.Tests
{
    [TestFixture]
    public class LanguageProcessorTests
    {

        [TestCase("Este es un texto escrito en español.", "spa")] // Spanish
        [TestCase("Ceci est un texte écrit en français.", "fra")] // French
        [TestCase("Dies ist ein in deutscher Sprache verfasster Text.", "deu")] // German
        [TestCase("Αυτό είναι ένα κείμενο γραμμένο στα ελληνικά.", "ell")] // Greek
        [TestCase("这是一段用中文写的文字。", "zho")] // Mandarin Chinese
        [TestCase("यह एक हिंदी में लिखा गया पाठ है।", "hin")] // Hindi
        [TestCase("هذا نص مكتوب باللغة العربية.", "ara")] // Arabic
        [TestCase("Este é um texto escrito em português.", "por")] // Portuguese
        [TestCase("Это текст, написанный на русском языке.", "rus")] // Russian
        [TestCase("یہ ایک اردو میں لکھا گیا متن ہے۔", "urd")] // Urdu
        public void DetectLanguage_FromText_ReturnsISO3Code(string input, string expectedOutput)
        {
            var iso3Code = LanguageProcessor.DetectLanguage(
                input, 
                "eng");

            Assert.That(iso3Code, Is.EqualTo(expectedOutput.ToLower()));
        }


        [TestCase("")]
        [TestCase("    ")]
        [TestCase("1234567890")]
        [TestCase("!@#$%^&*()")]
        [TestCase("😃😃😃😃😃")]
        public void DetectLanguage_InvalidOrEmptyInput_ReturnsFallback(string input)
        {         
            var result = LanguageProcessor.DetectLanguage(
                input, 
                "eng");

            Assert.That(result, Is.EqualTo(
                "eng"));
        }


        [TestCase("spa", "es")] // Spanish
        [TestCase("fra", "fr")] // French
        [TestCase("deu", "de")] // German
        [TestCase("ell", "el")] // Greek
        [TestCase("zho", "zh")] // Chinese
        [TestCase("hin", "hi")] // Hindi
        [TestCase("ara", "ar")] // Arabic
        [TestCase("por", "pt")] // Portuguese
        [TestCase("rus", "ru")] // Russian
        [TestCase("urd", "ur")] // Urdu
        public void ConvertLanguageIso3ToIso2_ReturnsExpectedIso2Code(string iso3, string expectedIso2)
        {
            var result = LanguageProcessor.ConvertLanguageIso3ToIso2(
                iso3,
                "eng");

            Assert.That(result, Is.EqualTo(expectedIso2));
        }


        [TestCase("")]
        [TestCase("    ")]
        [TestCase("XXX")] // Invalid code
        public void ConvertLanguageIso3ToIso2_InvalidOrEmpty_ReturnsFallback(string iso3)
        {
            var result = LanguageProcessor.ConvertLanguageIso3ToIso2(
                iso3,
                "en");

            Assert.That(result, Is.EqualTo(
                "en"));
        }

    }
}