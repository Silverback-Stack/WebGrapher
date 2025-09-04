using System;
using System.Globalization;
using LanguageDetection;

namespace Normalisation.Core.Processors
{
    public static class LanguageIdentifier
    {
        /// <summary>
        /// Retruns ISO 639-3 language code
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string DetectLanguage(string input, NormalisationSettings normalisationSettings)
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

        public static string ConvertLanguageIso3ToIso2(string iso3LanagugeCode, NormalisationSettings normalisationSettings)
        {
            if (string.IsNullOrWhiteSpace(iso3LanagugeCode))
                return normalisationSettings.Processors.DefaultLanguageIso2Code;

            var culture = CultureInfo
                .GetCultures(CultureTypes.NeutralCultures)
                .FirstOrDefault(c => c.ThreeLetterISOLanguageName.Equals(iso3LanagugeCode, StringComparison.OrdinalIgnoreCase));

            return culture?.TwoLetterISOLanguageName ?? normalisationSettings.Processors.DefaultLanguageIso2Code;

        }
    }
}
