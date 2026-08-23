using System;
using System.Globalization;
using LanguageDetection;

namespace Normalisation.Core.Processors
{
    public static class LanguageProcessor
    {
        /// <summary>
        /// Detects the language of text and returns its ISO 639-3 language code.
        /// </summary>
        public static string DetectLanguage(
            string input, 
            NormalisationSettings normalisationSettings)
        {
            if (string.IsNullOrWhiteSpace(input))
                return normalisationSettings.Processors.DefaultLanguageIso3Code;

            var detector = new LanguageDetector();
            detector.AddAllLanguages();

            try
            {
                var result = detector.Detect(input);

                if (result is null)
                    result = normalisationSettings.Processors.DefaultLanguageIso3Code;

                return result;
            }
            catch (Exception)
            {
                return normalisationSettings.Processors.DefaultLanguageIso3Code;
            }
        }


        /// <summary>
        /// Converts an ISO 639-3 language code to its ISO 639-1 equivalent.
        /// </summary>
        public static string ConvertLanguageIso3ToIso2(
            string iso3LanguageCode, 
            NormalisationSettings normalisationSettings)
        {
            if (string.IsNullOrWhiteSpace(iso3LanguageCode))
                return normalisationSettings.Processors.DefaultLanguageIso2Code;

            var culture = CultureInfo
                .GetCultures(CultureTypes.NeutralCultures)
                .FirstOrDefault(c => c.ThreeLetterISOLanguageName.Equals(
                    iso3LanguageCode, StringComparison.OrdinalIgnoreCase));

            return culture?.TwoLetterISOLanguageName ?? normalisationSettings.Processors.DefaultLanguageIso2Code;

        }
    }
}
