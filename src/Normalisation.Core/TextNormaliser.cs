using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Normalisation.Core
{
    public static class TextNormaliser
    {
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

        public static string RemoveDuplicateWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var distinctWords = words
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return string.Join(' ', distinctWords);
        }

    }
}
