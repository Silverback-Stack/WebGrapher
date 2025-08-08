using Normalisation.Core.Processors;

namespace Service.Normalisation.Tests
{
    [TestFixture]
    public class LanguageIdentifierTests
    {
        [SetUp]
        public void Setup()
        {
    
        }

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
            var iso3Code = LanguageIdentifier.DetectLanguage(input);

            Assert.That(iso3Code, Is.EqualTo(expectedOutput.ToLower()));
        }

        [TestCase("")]
        [TestCase("    ")]
        [TestCase("1234567890")]
        [TestCase("!@#$%^&*()")]
        [TestCase("😃😃😃😃😃")]
        public void DetectLanguage_InvalidOrEmptyInput_ReturnsDefault(string input)
        {
            var defaultIso3 = LanguageIdentifier.DEFAULT_LANGUAGE_ISO3_CODE;
            var result = LanguageIdentifier.DetectLanguage(input);

            Assert.That(result, Is.EqualTo(defaultIso3));
        }


        [TestFixture]
        public class LanguageConversionTests
        {
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
                var result = LanguageIdentifier.ConvertLanguageIso3ToIso2(iso3);
                Assert.That(result, Is.EqualTo(expectedIso2));
            }

            [TestCase("")]
            [TestCase("    ")]
            [TestCase("XXX")] // Invalid code
            public void ConvertLanguageIso3ToIso2_ReturnsDefault_ForInvalidOrEmpty(string iso3)
            {
                var defaultIso2 = LanguageIdentifier.DEFAULT_LANGUAGE_ISO2_CODE;
                var result = LanguageIdentifier.ConvertLanguageIso3ToIso2(iso3);
                Assert.That(result, Is.EqualTo(defaultIso2));
            }
        }


    }
}