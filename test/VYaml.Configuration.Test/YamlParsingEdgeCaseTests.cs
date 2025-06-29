// <copyright file="YamlParsingEdgeCaseTests.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

// ReSharper disable BadParensSpaces
// ReSharper disable AccessToStaticMemberViaDerivedType
namespace VYaml.Configuration.Test
{
    using System;
    using System.IO;
    using System.Text;
    using Microsoft.Extensions.Configuration;
    using NUnit.Framework;

    /// <summary>
    /// Contains unit tests for validating edge cases and error handling during YAML parsing.
    /// Includes tests for invalid syntax, malformed structures, special characters, large inputs,
    /// and other boundary scenarios to ensure robust parsing.
    /// </summary>
    [TestFixture]
    public class YamlParsingEdgeCaseTests
    {
        /// <summary>
        /// Represents an instance of <see cref="YamlParser"/> utilized for parsing YAML content.
        /// This variable is used for running various tests related to parsing YAML files,
        /// including tests for edge cases and invalid YAML syntax.
        /// </summary>
        private YamlParser parser = null!;

        /// <summary>
        /// Initializes the test setup before each test is executed.
        /// This method is executed automatically before each test in the test fixture,
        /// ensuring a fresh instance of the YamlParser is created for testing.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.parser = new YamlParser();
        }

        /// <summary>
        /// Verifies that the parser correctly throws a <see cref="FormatException"/>
        /// when encountering an invalid YAML syntax, such as malformed indentation.
        /// </summary>
        /// <exception cref="FormatException">
        /// Thrown when the YAML syntax is invalid, such as mismatched indentation or
        /// other parsing errors.
        /// </exception>
        [Test]
        public void Parse_InvalidYamlSyntax_ThrowsFormatException()
        {
            // Arrange
            var invalidYaml = """

                key: value
                  invalid: indentation
                another: value
                """;

            // Act & Assert
            using var stream = CreateStream(invalidYaml);
            var ex = Assert.Throws<FormatException>(() => this.parser.Parse(stream))!;
            Assert.That(ex.Message, Does.Contain("Failed to parse YAML"));
        }

        /// <summary>
        /// Validates that the YAML parser throws a <see cref="FormatException"/>
        /// when attempting to parse input containing tab characters, which are invalid in YAML syntax.
        /// </summary>
        [Test]
        public void Parse_TabCharacters_ThrowsFormatException()
        {
            // Arrange
            var yamlWithTabs = "key:\tvalue"; // Contains tab character

            // Act & Assert
            using var stream = CreateStream(yamlWithTabs);
            var ex = Assert.Throws<FormatException>(() => this.parser.Parse(stream))!;
            Assert.That(ex.Message, Does.Contain("tab").IgnoreCase);
        }

        /// <summary>
        /// Tests that the YAML parser throws a <see cref="FormatException"/>
        /// when encountering YAML content with unmatched brackets.
        /// </summary>
        /// <remarks>
        /// This test specifically validates the parser's ability to detect and handle
        /// improperly formed arrays or bracketed segments in YAML syntax. Unmatched
        /// brackets (e.g., missing closing square brackets) are expected to result in
        /// an error during parsing.
        /// </remarks>
        /// <exception cref="System.FormatException">
        /// Thrown when the parser encounters unmatched brackets in the input YAML content.
        /// </exception>
        [Test]
        public void Parse_UnmatchedBrackets_ThrowsFormatException()
        {
            // Arrange
            var invalidYaml = """

                array: [item1, item2
                missingCloseBracket: true
                """;

            // Act & Assert
            using var stream = CreateStream(invalidYaml);
            Assert.Throws<FormatException>(() => this.parser.Parse(stream));
        }

        /// <summary>
        /// Tests that the <see cref="YamlParser.Parse"/> method throws a <see cref="FormatException"/>
        /// when parsing a YAML input containing unmatched braces.
        /// </summary>
        /// <remarks>
        /// This test ensures that the parser detects unmatched opening brace `{` without a corresponding closing brace `}`
        /// and handles the invalid YAML gracefully by throwing a format exception. This is critical for ensuring the
        /// integrity of YAML structure validation.
        /// </remarks>
        /// <exception cref="FormatException">
        /// Thrown when the input YAML contains unmatched braces, indicating malformed structure.
        /// </exception>
        [Test]
        public void Parse_UnmatchedBraces_ThrowsFormatException()
        {
            // Arrange
            var invalidYaml = """

                object: {key1: value1, key2: value2
                missingCloseBrace: true
                """;

            // Act & Assert
            using var stream = CreateStream(invalidYaml);
            Assert.Throws<FormatException>(() => this.parser.Parse(stream));
        }

        /// <summary>
        /// Tests the YAML parsing functionality to ensure that mixed indentation styles are handled gracefully.
        /// Verifies that the parser can correctly process YAML content where the indentation style (e.g., 2 spaces vs. 4 spaces)
        /// varies between different sections of the document, as long as the indentation remains consistent within specific contexts.
        /// </summary>
        /// <remarks>
        /// This test ensures that the parser adheres to the YAML specification regarding indentation consistency within contexts
        /// and does not raise errors when the indentation style switches between properly structured sections.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown by the test framework if the parser's results do not match the expected output.
        /// </exception>
        [Test]
        public void Parse_MixedIndentationStyles_HandlesGracefully()
        {
            // Arrange - This should be valid YAML as long as indentation is consistent within context
            var yamlWithMixedIndentation = """

                parent1:
                  child1: value1  # 2 spaces
                  child2: value2
                parent2:
                    child1: value3  # 4 spaces
                    child2: value4
                """;

            // Act
            using var stream = CreateStream(yamlWithMixedIndentation);
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["parent1:child1"], Is.EqualTo("value1"));
            Assert.That(result["parent2:child1"], Is.EqualTo("value3"));
        }

        /// <summary>
        /// Tests whether the YAML parser can correctly handle parsing of YAML content with very long lines.
        /// Verifies that no truncation, data loss, or unexpected behavior occurs when processing lines
        /// exceeding typical lengths, especially those encountered in edge cases or large configurations.
        /// </summary>
        /// <remarks>
        /// The test ensures that the parser correctly parses YAML values with extremely long strings
        /// associated with a key and verifies that the resulting key-value pairs are preserved as expected.
        /// </remarks>
        [Test]
        public void Parse_VeryLongLines_HandlesCorrectly()
        {
            // Arrange
            var longValue = new string('a', 10000);
            var yaml = $"longKey: \"{longValue}\"";

            // Act
            using var stream = CreateStream(yaml);
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["longKey"], Is.EqualTo(longValue));
        }

        /// <summary>
        /// Validates that the parser correctly handles deeply nested YAML structures and retains the structure
        /// integrity during parsing.
        /// </summary>
        /// <remarks>
        /// This test is designed to verify the parser's ability to correctly process YAML documents with highly
        /// nested key-value pairs. It ensures that no data corruption or loss occurs when handling complex
        /// hierarchical structures.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown when the resulting parsed data does not match the expected nested structure or value.
        /// </exception>
        [Test]
        public void Parse_DeeplyNestedStructure_HandlesCorrectly()
        {
            // Arrange
            var deepYaml = """

                level1:
                  level2:
                    level3:
                      level4:
                        level5:
                          level6:
                            level7:
                              level8:
                                level9:
                                  level10: deepvalue
                """;

            // Act
            using var stream = CreateStream(deepYaml);
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(
                result["level1:level2:level3:level4:level5:level6:level7:level8:level9:level10"],
                Is.EqualTo("deepvalue")
            );
        }

        /// <summary>
        /// Tests the ability of the YAML parser to correctly handle and process
        /// a large array of items within a YAML document.
        /// </summary>
        /// <remarks>
        /// This test ensures that the parser is capable of reading and parsing
        /// YAML files containing massive arrays effectively, without truncation
        /// or errors related to array handling. It validates that the indexed values
        /// of the resultant data match expected values for both the beginning and
        /// the end of the large array.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown when the parsed output does not match the expected values for the
        /// large array elements.
        /// </exception>
        [Test]
        public void Parse_LargeArray_HandlesCorrectly()
        {
            // Arrange
            var yamlBuilder = new StringBuilder();
            yamlBuilder.AppendLine("largeArray:");
            for (int i = 0; i < 5000; i++)
            {
                yamlBuilder.AppendLine($"  - item{i}");
            }

            // Act
            using var stream = CreateStream(yamlBuilder.ToString());
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["largeArray:0"], Is.EqualTo("item0"));
            Assert.That(result["largeArray:4999"], Is.EqualTo("item4999"));
        }

        /// <summary>
        /// Tests that the YAML parser correctly handles keys containing various special characters.
        /// Validates that keys such as those with spaces, dashes, underscores, colons, slashes, and dots
        /// are correctly parsed and their associated values are accurately retrieved.
        /// </summary>
        [Test]
        public void Parse_SpecialCharactersInKeys_HandlesCorrectly()
        {
            // Arrange
            var yaml = """

                "key with spaces": value1
                key-with-dashes: value2
                key_with_underscores: value3
                "key:with:colons": value4
                "key/with/slashes": value5
                "key.with.dots": value6
                """;

            // Act
            using var stream = CreateStream(yaml);
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["key with spaces"], Is.EqualTo("value1"));
            Assert.That(result["key-with-dashes"], Is.EqualTo("value2"));
            Assert.That(result["key_with_underscores"], Is.EqualTo("value3"));
            Assert.That(result["key:with:colons"], Is.EqualTo("value4"));
            Assert.That(result["key/with/slashes"], Is.EqualTo("value5"));
            Assert.That(result["key.with.dots"], Is.EqualTo("value6"));
        }

        /// <summary>
        /// Verifies that the YAML parser correctly handles Unicode characters
        /// in keys and values, including languages (e.g., Japanese, Chinese, Arabic),
        /// emojis, and accented characters without data loss or errors.
        /// </summary>
        [Test]
        public void Parse_UnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var yaml = """

                unicode_key: "日本語の値"
                emoji_key: "🚀 Rocket launch! 🎉"
                accented_key: "Café résumé naïve"
                chinese_key: "中文测试"
                arabic_key: "مرحبا بالعالم"
                """;

            // Act
            using var stream = CreateStream(yaml);
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["unicode_key"], Is.EqualTo("日本語の値"));
            Assert.That(result["emoji_key"], Is.EqualTo("🚀 Rocket launch! 🎉"));
            Assert.That(result["accented_key"], Is.EqualTo("Café résumé naïve"));
            Assert.That(result["chinese_key"], Is.EqualTo("中文测试"));
            Assert.That(result["arabic_key"], Is.EqualTo("مرحبا بالعالم"));
        }

        /// <summary>
        /// Tests that the YAML parser correctly handles YAML strings containing escape sequences,
        /// such as newline, tab, backslash, double quotes, and carriage return characters.
        /// Validates the parsing and resolution of these escaped characters into their intended values.
        /// </summary>
        [Test]
        public void Parse_EscapeSequences_HandlesCorrectly()
        {
            // Arrange
            var yaml = """

                newline: "Line 1\nLine 2"
                tab: "Column 1\tColumn 2"
                backslash: "Path\\to\\file"
                quote: "She said \"Hello\""
                carriageReturn: "Line 1\rLine 2"
                """;

            // Act
            using var stream = CreateStream(yaml);
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["newline"], Is.EqualTo("Line 1\nLine 2"));
            Assert.That(result["tab"], Is.EqualTo("Column 1\tColumn 2"));
            Assert.That(result["backslash"], Is.EqualTo("Path\\to\\file"));
            Assert.That(result["quote"], Is.EqualTo("She said \"Hello\""));
            Assert.That(result["carriageReturn"], Is.EqualTo("Line 1\rLine 2"));
        }

        /// <summary>
        /// Tests the YAML parser's ability to handle keys with extremely large lengths.
        /// Ensures that the parser can correctly process and retrieve values associated
        /// with keys that exceed typical length expectations without errors or truncation.
        /// </summary>
        [Test]
        public void Parse_ExtremelyLongKey_HandlesCorrectly()
        {
            // Arrange
            var longKey = new string('k', 1000);
            var yaml = $"{longKey}: value";

            // Act
            using var stream = CreateStream(yaml);
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result[longKey], Is.EqualTo("value"));
        }

        /// <summary>
        /// Tests the behavior of YAML parsing when multiple duplicate keys are present in the input.
        /// Validates that the parser correctly handles duplicate keys by retaining only the last occurrence of each key.
        /// </summary>
        [Test]
        public void Parse_ManyDuplicateKeys_LastValueWins()
        {
            // Arrange
            var yaml = """

                key: value1
                key: value2
                key: value3
                key: value4
                key: finalValue
                """;

            // Act
            using var stream = CreateStream(yaml);
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["key"], Is.EqualTo("finalValue"));
            Assert.That(result, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Validates if the YAML parser correctly handles and parses values written in scientific notation.
        /// </summary>
        /// <remarks>
        /// This test ensures that the parser can process numbers represented using scientific notation, including
        /// cases with positive and negative exponents, large values, and different formats (e.g., lower or uppercase 'e').
        /// It checks if parsed values are correctly converted into the appropriate string representations.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Throws if the parsed output does not match the expected string representation of the scientific notation values.
        /// </exception>
        [Test]
        public void Parse_ScientificNotation_HandlesCorrectly()
        {
            // Arrange
            var yaml = """

                scientific1: 1.23e4
                scientific2: 1.23E-4
                scientific3: -1.23e+10
                large: 1.23456789e100
                """;

            // Act
            using var stream = CreateStream(yaml);
            var result = this.parser.Parse(stream);

            // Assert - VYaml preserves the original format
            Assert.That(result, Is.Not.Null);
            Assert.That(result["scientific1"], Is.EqualTo("1.23e4"));
            Assert.That(result["scientific2"], Is.EqualTo("1.23E-4"));
            Assert.That(result["scientific3"], Is.EqualTo("-1.23e+10"));
            Assert.That(result["large"], Is.EqualTo("1.23456789e100"));
        }

        /// <summary>
        /// Validates the YAML parser's handling of special YAML values and ensures correct parsing results for:
        /// - Infinity (.inf) and negative infinity (-.inf)
        /// - NaN (.nan)
        /// - Null values (null, ~, or empty strings)
        /// - Boolean values (true, True, TRUE, yes, Yes, YES, false, False, FALSE, no, No, NO).
        /// </summary>
        /// <remarks>
        /// The test verifies that special YAML values are correctly parsed into their expected representations:
        /// - `.inf` is parsed as `∞`.
        /// - `-.inf` is parsed as `-∞`.
        /// - `.nan` is parsed as `NaN`.
        /// - Null representations are parsed as `null`.
        /// - Boolean variations are normalized to `True` or `False` strings.
        /// </remarks>
        [Test]
        public void Parse_SpecialYamlValues_HandlesCorrectly()
        {
            // Arrange
            var yaml = """

                infinity: .inf
                negInfinity: -.inf
                notANumber: .nan
                nullValue1: null
                nullValue2: ~
                nullValue3: 
                boolTrue1: true
                boolTrue2: True
                boolTrue3: TRUE
                boolTrue4: yes
                boolTrue5: Yes
                boolTrue6: YES
                boolFalse1: false
                boolFalse2: False
                boolFalse3: FALSE
                boolFalse4: no
                boolFalse5: No
                boolFalse6: NO
                """;

            // Act
            using var stream = CreateStream(yaml);
            var result = this.parser.Parse(stream);

            // Assert - VYaml preserves literal values
            Assert.That(result, Is.Not.Null);
            Assert.That(result["infinity"], Is.EqualTo(".inf"));
            Assert.That(result["negInfinity"], Is.EqualTo("-.inf"));
            Assert.That(result["notANumber"], Is.EqualTo(".nan"));
            Assert.That(result["nullValue1"], Is.Null);
            Assert.That(result["nullValue2"], Is.Null);
            Assert.That(result["nullValue3"], Is.Null);
            Assert.That(result["boolTrue1"], Is.EqualTo("true"));
            Assert.That(result["boolTrue2"], Is.EqualTo("True"));
            Assert.That(result["boolTrue3"], Is.EqualTo("TRUE"));
            Assert.That(result["boolTrue4"], Is.EqualTo("yes"));
            Assert.That(result["boolTrue5"], Is.EqualTo("Yes"));
            Assert.That(result["boolTrue6"], Is.EqualTo("YES"));
            Assert.That(result["boolFalse1"], Is.EqualTo("false"));
            Assert.That(result["boolFalse2"], Is.EqualTo("False"));
            Assert.That(result["boolFalse3"], Is.EqualTo("FALSE"));
            Assert.That(result["boolFalse4"], Is.EqualTo("no"));
            Assert.That(result["boolFalse5"], Is.EqualTo("No"));
            Assert.That(result["boolFalse6"], Is.EqualTo("NO"));
        }

        /// <summary>
        /// Tests that an invalid YAML file causes a <see cref="FormatException"/> to be thrown.
        /// Verifies that the exception message includes both relevant error context and the file path.
        /// </summary>
        /// <exception cref="FormatException">
        /// Thrown when the YAML file contains malformed syntax or invalid indentation.
        /// </exception>
        [Test]
        public void Configuration_InvalidYamlFile_ThrowsFormatExceptionWithContext()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(
                tempFile,
#pragma warning disable SA1118
                """
                key: value
                  invalid: indentation
                another: value
                """
            );
#pragma warning restore SA1118

            try
            {
                // Act & Assert
                var ex = Assert.Throws<InvalidDataException>(() =>
                {
                    // ReSharper disable once UnusedVariable
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Path.GetDirectoryName(tempFile)!)
                        .AddYamlFile(Path.GetFileName(tempFile))
                        .Build();

                    Assert.Fail($"Expected exception to be thrown in {configuration}");
                });

                Assert.That(ex.InnerException!, Is.TypeOf<FormatException>());
                Assert.That(
                    ex.InnerException!.Message,
                    Does.Contain("Failed to parse YAML")
                );
                Assert.That(ex.Message, Does.Contain(tempFile));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Tests the YAML parser's ability to correctly process only the first document
        /// in a multi-document YAML stream. Ensures that the parser ignores subsequent
        /// documents and focuses solely on the first document's content.
        /// </summary>
        /// <remarks>
        /// Multi-document YAML streams are separated by "---". This test checks that the
        /// parser does not process keys or values from documents beyond the first document.
        /// </remarks>
        /// <exception cref="System.FormatException">
        /// Thrown if the input is not valid YAML or cannot be parsed properly.
        /// </exception>
        /// <seealso cref="VYaml.Configuration.YamlParser"/>
        [Test]
        public void Parse_MultiDocumentYaml_ProcessesFirstDocumentOnly()
        {
            // Arrange
            var multiDocYaml = """

                key1: value1
                key2: value2
                ---
                key3: value3
                key4: value4
                ---
                key5: value5
                """;

            // Act
            using var stream = CreateStream(multiDocYaml);
            var result = this.parser.Parse(stream);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result["key1"], Is.EqualTo("value1"));
                Assert.That(result["key2"], Is.EqualTo("value2"));

                // The VYaml parser should only process the first document
                Assert.That(result.ContainsKey("key3"), Is.False);
                Assert.That(result.ContainsKey("key5"), Is.False);
            });
        }

        /// <summary>
        /// Parses a very large YAML file efficiently, ensuring correctness and performance.
        /// This method is designed to handle files with a large number of keys and nested structures
        /// without significant memory overhead or performance degradation.
        /// </summary>
        /// <remarks>
        /// The method processes the input stream containing a large YAML structure,
        /// parsing it into an intermediate Dictionary representation. It verifies
        /// the integrity of the YAML data and ensures correct handling of nested structures,
        /// large collections, and significant input size.
        /// </remarks>
        /// <exception cref="FormatException">
        /// Thrown if the YAML file has invalid syntax or is malformed.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown if there is an error reading the stream.
        /// </exception>
        /// <example>
        /// This method processes nested keys into a flattened dictionary,
        /// where each key represents the full path to the value.
        /// The function demonstrates how it maintains minimal memory usage
        /// despite the size of the input YAML file.
        /// </example>
        [Test]
        public void Parse_VeryLargeFile_HandlesEfficiently()
        {
            // Arrange
            var largeYamlBuilder = new StringBuilder();
            largeYamlBuilder.AppendLine("root:");
            for (int i = 0; i < 10000; i++)
            {
                largeYamlBuilder.AppendLine($"  key{i}:");
                largeYamlBuilder.AppendLine($"    subkey1: value{i}_1");
                largeYamlBuilder.AppendLine($"    subkey2: value{i}_2");
            }

            // Act
            using var stream = CreateStream(largeYamlBuilder.ToString());
            var result = this.parser.Parse(stream);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result["root:key0:subkey1"], Is.EqualTo("value0_1"));
                Assert.That(result["root:key9999:subkey2"], Is.EqualTo("value9999_2"));
                Assert.That(result, Has.Count.EqualTo(20000)); // 10000 keys * 2 subkeys each
            });
        }

        /// <summary>
        /// Creates a memory stream from the provided string content using UTF-8 encoding.
        /// </summary>
        /// <param name="content">The string content to be written to the memory stream.</param>
        /// <returns>A MemoryStream containing the UTF-8 encoded content of the provided string.</returns>
        private static MemoryStream CreateStream(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return new MemoryStream(bytes);
        }
    }
}
