using System;
using Normalisation.Core.Processors;

namespace Normalisation.Core.Tests
{
    [TestFixture]
    public class TextNormaliserTests
    {
        [SetUp]
        public void Setup()
        {

        }

        [TestCase("HELLO WORLD", "hello world")]
        [TestCase("MiXeD CaSe", "mixed case")]
        [TestCase("", "")]
        public void ToLowerCase_ReturnsLowerCase(string input, string expected)
        {
            var result = TextProcessor.ToLowerCase(input);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("Hello, world!", "Hello world")]
        [TestCase("No punctuation", "No punctuation")]
        [TestCase("", "")]
        [TestCase("!@#$%^&*()", "$^")] // $^ are not punctuation chars
        public void RemovePunctuation_RemovesPunctuation(string input, string expected)
        {
            var result = TextProcessor.RemovePunctuation(input);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("Hello! &World@2023", "Hello World2023")]
        [TestCase("Remove_special#chars$", "Removespecialchars")]
        [TestCase("", "")]
        public void RemoveSpecialCharacters_RemovesNonLetterOrDigit(string input, string expected)
        {
            var result = TextProcessor.RemoveSpecialCharacters(input);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("Hello    world", "Hello world")]
        [TestCase("Tabs\tand\nnewlines", "Tabs and newlines")]
        [TestCase("   Leading and trailing   ", "Leading and trailing")]
        [TestCase("", "")]
        [TestCase("   ", "")]
        public void CollapseWhitespace_CollapsesCorrectly(string input, string expected)
        {
            var result = TextProcessor.CollapseWhitespace(input);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("Hello world", 5, "Hello")]
        [TestCase("Short", 10, "Short")]
        public void Truncate_ReturnsTruncatedOrOriginal(string input, int maxLength, string expected)
        {
            var result = TextProcessor.LimitTextLength(input, maxLength);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("this is is a test test", "this is a test")]
        [TestCase("hello Hello HELLO", "hello")]
        [TestCase("", "")]
        [TestCase("no duplicates here", "no duplicates here")]
        public void RemoveDuplicateWords_RemovesDuplicatesIgnoringCase(string input, string expected)
        {
            var result = TextProcessor.RemoveDuplicateWords(input);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("one two three four five six", 13, "one two three")]
        [TestCase("short text", 20, "short text")]
        [TestCase("one two three", 8, "one two")]
        [TestCase("verylongword", 5, "veryl")]
        public void LimitTextLength_ReturnsExpectedText(
            string input,
            int maxLength,
            string expected)
        {
            var result = TextProcessor.LimitTextLength(input, maxLength);

            Assert.That(result, Is.EqualTo(expected));
        }

    }
}
