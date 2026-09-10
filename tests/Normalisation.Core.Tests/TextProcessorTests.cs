using System;
using Normalisation.Core.Processors;

namespace Normalisation.Core.Tests
{
    [TestFixture]
    public class TextProcessorTests
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


        [TestCase("one 123 two 2024 three", "one two three")]
        [TestCase("123 456", "")]
        [TestCase("no numbers here", "no numbers here")]
        [TestCase("", "")]
        public void RemoveNumericalWords_RemovesNumericStrings(
            string input,
            string expected)
        {
            var result = TextProcessor.RemoveNumericStrings(input);

            Assert.That(result, Is.EqualTo(expected));
        }


        [Test]
        public void ExtractTags_ReturnsMostFrequentWords()
        {
            var tags = TextProcessor.ExtractTags(
                "apple banana apple orange banana apple", maxTags: 2)
                .ToList();

            Assert.That(tags, Is.EqualTo(new[]
            {
                "apple",
                "banana"
            }));
        }


        [Test]
        public void ExtractTags_WithEqualFrequency_ReturnsAlphabetically()
        {
            var tags = TextProcessor.ExtractTags(
                "orange banana apple", maxTags: 3)
                .ToList();

            Assert.That(tags, Is.EqualTo(new[]
            {
                "apple",
                "banana",
                "orange"
            }));
        }


        [Test]
        public void ExtractTags_WithNullText_ReturnsEmpty()
        {
            var tags = TextProcessor.ExtractTags(
                null!, maxTags: 5);

            Assert.That(tags, Is.Empty);
        }


        [Test]
        public void SplitLines_ReturnsIndividualLines()
        {
            var lines = TextProcessor.SplitLines(
                "First line\r\nSecond line\nThird line")
                .ToList();

            Assert.That(lines, Is.EqualTo(new[]
            {
                "First line",
                "Second line",
                "Third line"
            }));
        }


        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void SplitLines_WithEmptyText_ReturnsEmpty(string? input)
        {
            var lines = TextProcessor.SplitLines(input);

            Assert.That(lines, Is.Empty);
        }

    }
}
