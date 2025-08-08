using System;
using System.Globalization;
using LanguageDetection;

namespace Normalisation.Core.Processors
{
    public static class LanguageIdentifier
    {
        public const string DEFAULT_LANGUAGE_ISO2_CODE = "en";
        public const string DEFAULT_LANGUAGE_ISO3_CODE = "eng";

        /// <summary>
        /// Retruns ISO 639-3 language code
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string DetectLanguage(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return DEFAULT_LANGUAGE_ISO3_CODE;

            var detector = new LanguageDetector();
            detector.AddAllLanguages();

            try
            {
                var result = detector.Detect(input);

                if (result is null)
                    result = DEFAULT_LANGUAGE_ISO3_CODE;

                return result;
            }
            catch (Exception)
            {
                return DEFAULT_LANGUAGE_ISO3_CODE;
            }
        }

        public static string ConvertLanguageIso3ToIso2(string iso3LanagugeCode)
        {
            if (string.IsNullOrWhiteSpace(iso3LanagugeCode))
                return DEFAULT_LANGUAGE_ISO2_CODE;

            var culture = CultureInfo
                .GetCultures(CultureTypes.NeutralCultures)
                .FirstOrDefault(c => c.ThreeLetterISOLanguageName.Equals(iso3LanagugeCode, StringComparison.OrdinalIgnoreCase));

            return culture?.TwoLetterISOLanguageName ?? DEFAULT_LANGUAGE_ISO2_CODE;

        }
    }
}
