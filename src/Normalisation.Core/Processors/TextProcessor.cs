using System;
using System.Text.RegularExpressions;

namespace Normalisation.Core.Processors
{
    public static class TextProcessor
    {

        /// <summary>
        /// Converts text to lowercase consistently regardless of the system culture.
        /// </summary>
        public static string ToLowerCase(string text)
        {
            return text.ToLowerInvariant();
        }


        /// <summary>
        /// Removes punctuation characters from text.
        /// </summary>
        public static string RemovePunctuation(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return new string(text.Where(c => !char.IsPunctuation(c)).ToArray());
        }


        /// <summary>
        /// Removes all characters except letters, numbers, and whitespace from text.
        /// </summary>
        public static string RemoveSpecialCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return new string(text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
        }


        /// <summary>
        /// Replaces consecutive whitespace characters with a single space.
        /// </summary>
        public static string CollapseWhitespace(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Replace all runs of whitespace (spaces, tabs, newlines) with a single space
            var collapsed = Regex.Replace(text, @"\s+", " ");

            // Trim leading/trailing spaces
            return collapsed.Trim();
        }


        /// <summary>
        /// Limits text length without truncating words where possible.
        /// </summary>
        public static string LimitTextLength(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (text.Length <= maxLength)
                return text;

            var truncated = text.Substring(0, maxLength);

            // If the next character is whitespace, no word has been truncated.
            if (char.IsWhiteSpace(text[maxLength]))
                return truncated.TrimEnd();

            var lastSpace = truncated.LastIndexOf(' ');

            if (lastSpace > 0)
                truncated = truncated.Substring(0, lastSpace);

            return truncated;
        }


        /// <summary>
        /// Removes duplicate words while preserving their original order.
        /// </summary>
        public static string RemoveDuplicateWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var distinctWords = words
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return string.Join(' ', distinctWords);
        }


        /// <summary>
        /// Removes words that contain only numerical values, such as IDs, counts and years.
        /// </summary>
        public static string RemoveNumericalWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var filteredWords = text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !int.TryParse(word, out _));

            return string.Join(' ', filteredWords);
        }


        /// <summary>
        /// Extracts the most frequently occurring words as tags.
        /// </summary>
        public static IEnumerable<string> ExtractTags(string text, int maxTags)
        {
            if (text == null) return Enumerable.Empty<string>();

            var words = text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim());

            var tags = words
                .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Word = g.Key, Count = g.Count() })
                .OrderByDescending(k => k.Count)
                .ThenBy(k => k.Word) // tie-breaker by alphabetical
                .Take(maxTags)
                .Select(k => k.Word);

            return tags;
        }

    }
}
