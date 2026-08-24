using StopWord;

namespace Normalisation.Core.Processors
{
    public static class StopWordProcessor
    {

        /// <summary>
        /// Removes common stop words from text for the specified language.
        /// </summary>
        public static string RemoveStopWords(
            string input, 
            string iso3LanguageCode, 
            string fallbackIso2LanguageCode)
        {
            if (string.IsNullOrEmpty(iso3LanguageCode) || string.IsNullOrEmpty(input))
                return input;

            var iso2LanguageCode = LanguageProcessor.ConvertLanguageIso3ToIso2(
                iso3LanguageCode, fallbackIso2LanguageCode);

            try
            {
                var stopWords = StopWords.GetStopWords(iso2LanguageCode);

                if (stopWords == null || stopWords.Count() == 0)
                    return input;

                var words = input.Split(
                    ' ', StringSplitOptions.RemoveEmptyEntries);

                var filtered = words.Where(word => 
                    !stopWords.Contains(
                        word, StringComparer.OrdinalIgnoreCase));

                return string.Join(' ', filtered);
            }
            catch (Exception)
            {
                // Return the original text if stop words are unavailable for the language.
                return input;
            }

        }

    }
}
