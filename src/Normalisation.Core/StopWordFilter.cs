
using StopWord;

namespace Normalisation.Core
{
    public static class StopWordFilter
    {

        public static string RemoveStopWords(string input, string iso2LanguageCode)
        {
            var iso3 = LanguageIdentifier.DetectLanguage(input);
            var iso2 = LanguageIdentifier.ConvertLanguageIso3ToIso2(iso3);

            if (string.IsNullOrEmpty(iso2LanguageCode) || string.IsNullOrEmpty(input))
                return input;

            try
            {
                var stopWords = StopWords.GetStopWords(iso2LanguageCode);

                if (stopWords == null || stopWords.Count() == 0)
                    return input;

                var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var filtered = words.Where(word => !stopWords.Contains(word, StringComparer.OrdinalIgnoreCase));

                return string.Join(' ', filtered);
            }
            catch (Exception)
            {
                return input;
            }

        }

    }
}
