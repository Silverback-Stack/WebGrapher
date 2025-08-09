using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Normalisation.Core.Processors
{
    public static class TextNormaliser
    {

        public static string DecodeHtml(string text)
        {
            return WebUtility.HtmlDecode(text);
        }

        public static string ToLowerCase(string text)
        {
            return text.ToLower();
        }

        public static string RemovePunctuation(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return new string(text.Where(c => !char.IsPunctuation(c)).ToArray());
        }

        public static string RemoveSpecialCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return new string(text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
        }

        public static string CollapseWhitespace(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Replace all runs of whitespace (spaces, tabs, newlines) with a single space
            var collapsed = Regex.Replace(text, @"\s+", " ");

            // Trim leading/trailing spaces
            return collapsed.Trim();
        }

        public static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (text.Length < maxLength)
                return text;

            return text.Substring(0, maxLength);
        }

        public static string TruncateToWords(string text, int maxWords)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= maxWords)
                return text;

            return string.Join(' ', words.Take(maxWords));
        }

        public static string RemoveDuplicateWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var distinctWords = words
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return string.Join(' ', distinctWords);
        }

        public static string CondenseKeywords(string text, int limit)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var distinctWords = words
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return string.Join(' ', distinctWords);
        }

        public static string RemoveNumericalWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var filteredWords = text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !int.TryParse(word, out _));

            return string.Join(' ', filteredWords);
        }

        public static IEnumerable<string> ExtractTags(string text, int maxTags)
        {
            if (text == null) return Enumerable.Empty<string>();

            var keywords = text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim());

            var tags = keywords
                .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Keyword = g.Key, Count = g.Count() })
                .OrderByDescending(k => k.Count)
                .ThenBy(k => k.Keyword) // tie-breaker by alphabetical
                .Take(maxTags)
                .Select(k => k.Keyword);

            return tags;
        }

    }
}
