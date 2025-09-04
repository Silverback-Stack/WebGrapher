using StopWord;

namespace Normalisation.Core.Processors
{
    public static class StopWordFilter
    {

        public static string RemoveStopWords(string input, string iso3LanguageCode, NormalisationSettings normalisationSettings)
        {
            if (string.IsNullOrEmpty(iso3LanguageCode) || string.IsNullOrEmpty(input))
                return input;

            var iso2 = LanguageIdentifier.ConvertLanguageIso3ToIso2(iso3LanguageCode, normalisationSettings);

            try
            {
                var stopWords = StopWords.GetStopWords(iso2);

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
