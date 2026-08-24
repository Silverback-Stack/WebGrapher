using System;
using System.Globalization;
using LanguageDetection;

namespace Normalisation.Core.Processors
{
    public static class LanguageProcessor
    {
        /// <summary>
        /// Detects the language of text and returns its ISO 639-3 language code.
        /// Returns the fallback code when detection is not possible.
        /// </summary>
        public static string DetectLanguage(
            string input, 
            string fallbackIso3Code)
        {
            if (string.IsNullOrWhiteSpace(input))
                return fallbackIso3Code;

            var detector = new LanguageDetector();

            detector.AddAllLanguages();

            try
            {
                return detector.Detect(input) ?? fallbackIso3Code;
            }
            catch (Exception)
            {
                return fallbackIso3Code;
            }
        }


        /// <summary>
        /// Converts an ISO 639-3 language code to its ISO 639-1 equivalent.
        /// Returns the fallback code when conversion is not possible.
        /// </summary>
        public static string ConvertLanguageIso3ToIso2(
            string iso3Code, 
            string fallbackIso2Code)
        {
            if (string.IsNullOrWhiteSpace(iso3Code))
                return fallbackIso2Code;

            var culture = CultureInfo
                .GetCultures(CultureTypes.NeutralCultures)
                .FirstOrDefault(c => c.ThreeLetterISOLanguageName.Equals(
                    iso3Code, StringComparison.OrdinalIgnoreCase));

            return culture?.TwoLetterISOLanguageName ?? fallbackIso2Code;

        }
    }
}
