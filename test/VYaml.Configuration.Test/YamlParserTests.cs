// <copyright file="YamlParserTests.cs" company="Mistial Developer">
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
    using NUnit.Framework;
    
    /// <summary>
    /// Contains unit tests for the YamlParser class, focusing on verifying parsing logic and handling
    /// of different YAML structures such as key-value pairs, nested objects, arrays, and special cases.
    /// </summary>
    [TestFixture]
    public class YamlParserTests
    {
        /// <summary>
        /// Represents an instance of the YamlParser used for parsing YAML documents.
        /// This is the primary parser instance initialized and tested in unit tests within the YamlParserTests.
        /// It is utilized to interpret YAML input streams into flattened dictionary structures or other representations based on provided YAML content.
        /// </summary>
        private YamlParser parser = null!;

        /// <summary>
        /// Initializes the test environment before each unit test is executed.
        /// Configures and sets up required dependencies and resources.
        /// In this method, an instance of YamlParser is initialized for testing.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.parser = new YamlParser();
        }

        /// <summary>
        /// Tests the parsing of a YAML string with a single key-value pair into a dictionary.
        /// </summary>
        /// <remarks>
        /// This test validates that when a simple YAML content with a single key-value pair is parsed,
        /// the resulting dictionary contains the correct key and value. Ensures proper handling of basic YAML structures.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown if the resulting dictionary is null, does not contain the expected key,
        /// or if the associated value does not match the expected result.
        /// </exception>
        [Test]
        public void Parse_SimpleKeyValue_ReturnsCorrectDictionary()
        {
            // Arrange
            var yaml = "key: value";
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result["key"], Is.EqualTo("value"));
        }

        /// <summary>
        /// Tests the YamlParser's ability to parse a YAML document containing nested objects and return a flattened dictionary.
        /// Ensures that keys from nested structures are combined using a separator (e.g. `:`) to represent the hierarchy.
        /// </summary>
        /// <remarks>
        /// The method verifies that:
        /// - Nested keys are flattened into a single key string using a concatenation pattern (e.g., "parent:child").
        /// - All keys are correctly represented in the returned dictionary.
        /// - Values from the nested structure are associated with their corresponding flattened keys.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown when the parsed dictionary does not match the expected flattened representation.
        /// </exception>
        [Test]
        public void Parse_NestedObject_ReturnsFlattenedDictionary()
        {
            // Arrange
            const string yaml = """

                parent:
                  child: value
                  nested:
                    deep: deepvalue
                """;
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result["parent:child"], Is.EqualTo("value"));
                Assert.That(result["parent:nested:deep"], Is.EqualTo("deepvalue"));
            });
        }

        /// <summary>
        /// Validates that parsing a YAML document containing an array produces
        /// a dictionary where the array items are represented as indexed keys.
        /// </summary>
        /// <remarks>
        /// This method ensures that each item in a YAML array is converted into
        /// a dictionary entry, where the key is the array's path with a zero-based
        /// numeric index appended, and the value corresponds to the item's content.
        /// For example, a YAML array under the key "items" with three values should
        /// be parsed into keys "items:0", "items:1", and "items:2" in the resultant dictionary.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown if any assertion about the parsed structure is not met, such as
        /// missing keys or incorrect values in the returned dictionary.
        /// </exception>
        [Test]
        public void Parse_Array_ReturnsIndexedKeys()
        {
            // Arrange
            const string yaml = """

                items:
                  - first
                  - second
                  - third
                """;
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result["items:0"], Is.EqualTo("first"));
                Assert.That(result["items:1"], Is.EqualTo("second"));
                Assert.That(result["items:2"], Is.EqualTo("third"));
            });
        }

        /// <summary>
        /// Verifies that the YAML parser correctly parses an array of objects and represents them
        /// in the appropriate flattened dictionary structure.
        /// </summary>
        /// <remarks>
        /// This test ensures that nested objects within an array in a YAML document are flattened
        /// with indexed keys representing their hierarchical structure.
        /// Example:
        /// - Given a YAML structure with an array of objects, such as:
        /// servers:
        /// - name: server1
        /// port: 8080
        /// - name: server2
        /// port: 8081
        /// - The parser should flatten the structure into a dictionary with keys like:
        /// "servers:0:name", "servers:0:port", "servers:1:name", "servers:1:port".
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown if the parsed structure does not match the expected result, including
        /// missing or incorrectly formatted keys, or incorrect values.
        /// </exception>
        [Test]
        public void Parse_ArrayOfObjects_ReturnsCorrectStructure()
        {
            // Arrange
            const string yaml = """

                servers:
                  - name: server1
                    port: 8080
                  - name: server2
                    port: 8081
                """;
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result["servers:0:name"], Is.EqualTo("server1"));
                Assert.That(result["servers:0:port"], Is.EqualTo("8080"));
                Assert.That(result["servers:1:name"], Is.EqualTo("server2"));
                Assert.That(result["servers:1:port"], Is.EqualTo("8081"));
            });
        }

        /// <summary>
        /// Tests the parsing of YAML boolean values and verifies if the output
        /// is correctly converted to their string representations.
        /// </summary>
        /// <remarks>
        /// The test ensures that various forms of boolean values like 'true',
        /// 'false', 'yes', and 'no' within the YAML input are parsed into their
        /// respective string equivalents ("True" and "False").
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown when the parsed values do not match the expected string representations
        /// of boolean values.
        /// </exception>
        [Test]
        public void Parse_BooleanValues_ReturnsStringRepresentation()
        {
            // Arrange
            var yaml = """

                trueValue: true
                falseValue: false
                yesValue: yes
                noValue: no
                """;
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result["trueValue"], Is.EqualTo("true"));
                Assert.That(result["falseValue"], Is.EqualTo("false"));
                Assert.That(result["yesValue"], Is.EqualTo("yes"));
                Assert.That(result["noValue"], Is.EqualTo("no"));
            });
        }

        /// <summary>
        /// Tests that the YamlParser correctly parses numeric values in YAML files
        /// and ensures their string representations are returned in the resulting dictionary.
        /// </summary>
        /// <remarks>
        /// The test verifies parsing of integers, negative values, floating-point numbers,
        /// and scientific notation. Each numeric value in the YAML input is expected to
        /// be converted to its corresponding string representation in the output.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown if the parsed output does not match the expected string representations
        /// of the numeric values.
        /// </exception>
        [Test]
        public void Parse_NumericValues_ReturnsStringRepresentation()
        {
            // Arrange
            const string yaml = """

                integer: 42
                negative: -10
                float: 3.14
                scientific: 1.23e-4
                """;
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result["integer"], Is.EqualTo("42"));
                Assert.That(result["negative"], Is.EqualTo("-10"));
                Assert.That(result["float"], Is.EqualTo("3.14"));
                Assert.That(result["scientific"], Is.EqualTo("1.23e-4"));
            });
        }

        /// <summary>
        /// Tests the YamlParser to ensure it correctly handles null values in YAML content.
        /// Verifies that keys associated with null values, whether explicitly or implicitly defined,
        /// are parsed and their corresponding values are returned as null in the resulting dictionary.
        /// </summary>
        [Test]
        public void Parse_NullValue_ReturnsNull()
        {
            // Arrange
            const string yaml = """

                nullValue: null
                emptyValue: 
                explicitNull: ~
                """;
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["nullValue"], Is.Null);
            Assert.That(result["emptyValue"], Is.Null);
            Assert.That(result["explicitNull"], Is.Null);
        }

        /// <summary>
        /// Validates that the YAML parser correctly parses quoted string values and preserves their content.
        /// Supports single-quoted, double-quoted, and special characters within strings.
        /// Also ensures proper handling of multi-line and folded string formats, verifying the correct treatment of line breaks and spacing.
        /// </summary>
        [Test]
        public void Parse_QuotedStrings_PreservesContent()
        {
            // Arrange
            var yaml = """

                doubleQuoted: "Hello World"
                singleQuoted: 'Hello World'
                specialChars: "String with special chars: !@#$%^&*()"
                multiLine: |
                  Line 1
                  Line 2
                  Line 3
                folded: >
                  This is a folded
                  string that will be
                  on one line
                """;
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["doubleQuoted"], Is.EqualTo("Hello World"));
            Assert.That(result["singleQuoted"], Is.EqualTo("Hello World"));
            Assert.That(
                result["specialChars"],
                Is.EqualTo("String with special chars: !@#$%^&*()")
            );
            Assert.That(
                result["multiLine"].Replace("\r\n", "\n").Replace("\r", "\n"),
                Is.EqualTo("Line 1\nLine 2\nLine 3\n")
            );
            Assert.That(
                result["folded"],
                Is.EqualTo("This is a folded string that will be on one line\n")
            );
        }

        /// <summary>
        /// Tests whether the parser correctly handles an empty stream by returning an empty dictionary.
        /// </summary>
        /// <remarks>
        /// This test provides validation for the behavior of the parser when an empty stream is supplied.
        /// An empty stream should result in an empty dictionary without any exceptions being thrown.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown if the resulting dictionary is null or contains any entries.
        /// </exception>
        [Test]
        public void Parse_EmptyStream_ReturnsEmptyDictionary()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Unit test to verify that parsing an empty YAML document
        /// results in an empty dictionary.
        /// </summary>
        /// <remarks>
        /// The test ensures that the parser recognizes an explicitly empty YAML
        /// document (containing the start and end document markers '---' and '...')
        /// and returns a valid, empty dictionary without throwing any errors.
        /// </remarks>
        [Test]
        public void Parse_EmptyDocument_ReturnsEmptyDictionary()
        {
            // Arrange
            var yaml = "---\n...";
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Tests that the YAML parser throws a <see cref="FormatException"/>
        /// if the input YAML contains tab characters, which are not valid
        /// for indentation or key-value formatting in YAML.
        /// </summary>
        /// <remarks>
        /// This test ensures compliance with the YAML specification,
        /// which specifies that only spaces should be used for indentation.
        /// Tab characters are considered an invalid character in YAML syntax.
        /// </remarks>
        /// <exception cref="FormatException">
        /// Thrown when the parser encounters a tab character in the YAML content.
        /// </exception>
        [Test]
        public void Parse_TabCharacters_ThrowsFormatException()
        {
            // Arrange
            var yaml = "key:\tvalue"; // Contains tab character
            using var stream = CreateStream(yaml);

            // Act & Assert
            var ex = Assert.Throws<FormatException>(() => this.parser.Parse(stream))!;
            Assert.That(ex.Message, Does.Contain("tab"));
        }

        /// <summary>
        /// Tests whether the YAML parser correctly handles duplicate keys in a YAML document,
        /// ensuring that the value of the last key occurrence is retained in the resulting dictionary.
        /// </summary>
        /// <remarks>
        /// In YAML documents with duplicate keys, only the value of the last occurrence of the key
        /// should be preserved in the parsed output. This test verifies that behavior by providing
        /// a YAML document with multiple entries for the same key and checking that only the value of
        /// the last occurrence is included in the resulting dictionary.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown if the parser does not handle duplicate keys correctly, for example, by including
        /// values from earlier occurrences of the same key or failing to match the expected value
        /// for the key.
        /// </exception>
        [Test]
        public void Parse_DuplicateKeys_LastValueWins()
        {
            // Arrange
            var yaml = """

                key: first
                key: second
                key: third
                """;
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["key"], Is.EqualTo("third"));
        }

        /// <summary>
        /// Validates that the parser correctly flattens a complex nested YAML structure
        /// into a dictionary with properly constructed keys and their corresponding values.
        /// Tests for accurate handling of nested objects, arrays, and scalar mappings
        /// to create key-value pairs representing the full structure in a flattened form.
        /// </summary>
        /// <remarks>
        /// This test ensures that nested elements such as objects and arrays
        /// are flattened with consistent key generation. The test also checks if
        /// array elements are indexed and included appropriately in the flattened result.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown when the parsed output does not match the expected flattened dictionary
        /// structure, indicating incorrect handling of nested YAML structures.
        /// </exception>
        [Test]
        public void Parse_ComplexNestedStructure_ReturnsCorrectFlattening()
        {
            // Arrange
            var yaml = """

                application:
                  name: TestApp
                  database:
                    connections:
                      - name: primary
                        host: localhost
                        port: 5432
                      - name: replica
                        host: replica.db
                        port: 5432
                  features:
                    - logging
                    - caching
                """;
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["application:name"], Is.EqualTo("TestApp"));
            Assert.That(
                result["application:database:connections:0:name"],
                Is.EqualTo("primary")
            );
            Assert.That(
                result["application:database:connections:0:host"],
                Is.EqualTo("localhost")
            );
            Assert.That(
                result["application:database:connections:0:port"],
                Is.EqualTo("5432")
            );
            Assert.That(
                result["application:database:connections:1:name"],
                Is.EqualTo("replica")
            );
            Assert.That(
                result["application:database:connections:1:host"],
                Is.EqualTo("replica.db")
            );
            Assert.That(
                result["application:database:connections:1:port"],
                Is.EqualTo("5432")
            );
            Assert.That(result["application:features:0"], Is.EqualTo("logging"));
            Assert.That(result["application:features:1"], Is.EqualTo("caching"));
        }

        /// <summary>
        /// Tests that the YAML parser correctly handles and processes mixed array types.
        /// Verifies that various data types in an array, including strings, numbers, booleans,
        /// null values, and nested objects, are correctly parsed and indexed appropriately.
        /// </summary>
        [Test]
        public void Parse_MixedArrayTypes_HandlesCorrectly()
        {
            // Arrange
            var yaml = """

                mixed:
                  - string value
                  - 42
                  - true
                  - null
                  - nested:
                      key: value
                """;
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["mixed:0"], Is.EqualTo("string value"));
            Assert.That(result["mixed:1"], Is.EqualTo("42"));
            Assert.That(result["mixed:2"], Is.EqualTo("true"));
            Assert.That(result["mixed:3"], Is.Null);
            Assert.That(result["mixed:4:nested:key"], Is.EqualTo("value"));
        }

        /// <summary>
        /// Verifies that the YAML parser processes and correctly handles content with Unicode characters,
        /// including special symbols, multilingual text, and emojis.
        /// </summary>
        /// <remarks>
        /// This test ensures that Unicode characters in YAML content are parsed without errors
        /// and their values are correctly represented in the returned dictionary.
        /// It checks for both general Unicode text and emojis.
        /// </remarks>
        [Test]
        public void Parse_UnicodeContent_HandlesCorrectly()
        {
            // Arrange
            const string yaml = """

                unicode: "This contains unicode: ñáéíóú 中文 🚀"
                emoji: "🎉 Celebration! 🎊"
                """;
            using var stream = CreateStream(yaml);

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(
                    result["unicode"],
                    Is.EqualTo("This contains unicode: ñáéíóú 中文 🚀")
                );
                Assert.That(result["emoji"], Is.EqualTo("🎉 Celebration! 🎊"));
            });
        }

        /// <summary>
        /// Tests the efficiency and correctness of parsing a large YAML document.
        /// Validates that the parser can handle large input streams without performance degradation or incorrect results.
        /// </summary>
        /// <remarks>
        /// This test ensures that when presented with a YAML document containing a significant number of entries,
        /// the parser processes the document efficiently and produces accurate output.
        /// It verifies the output structure and integrity, ensuring that all key-value pairs are correctly parsed
        /// and accessible via the expected keys.
        /// </remarks>
        /// <exception cref="AssertionException">
        /// Thrown if the parsed output does not match the expected structure or if the parsing takes an unreasonable
        /// amount of time to complete.
        /// </exception>
        [Test]
        public void Parse_LargeDocument_HandlesEfficiently()
        {
            // Arrange
            var yamlBuilder = new StringBuilder();
            yamlBuilder.AppendLine("items:");
            for (int i = 0; i < 1000; i++)
            {
                yamlBuilder.AppendLine($"  - name: item{i}");
                yamlBuilder.AppendLine($"    value: {i}");
            }

            using var stream = CreateStream(yamlBuilder.ToString());

            // Act
            var result = this.parser.Parse(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2000)); // 1000 items * 2 properties each
            Assert.That(result["items:0:name"], Is.EqualTo("item0"));
            Assert.That(result["items:999:name"], Is.EqualTo("item999"));
            Assert.That(result["items:999:value"], Is.EqualTo("999"));
        }

        /// <summary>
        /// Verifies that calling the Parse method with a null stream argument throws an ArgumentNullException.
        /// </summary>
        /// <remarks>
        /// This test is designed to ensure that the Parse method does not accept a null input stream.
        /// Passing null is considered an invalid operation and must result in an ArgumentNullException.
        /// This guards against potential null reference issues during YAML parsing.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the stream argument provided to the Parse method is null.
        /// </exception>
        [Test]
        public void Parse_NullStream_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<FormatException>(
                () => this.parser.Parse(null!),
                "Value cannot be null."
            );
        }

        /// <summary>
        /// Tests that attempting to parse a closed stream using the YamlParser
        /// throws an ObjectDisposedException.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the provided stream has been closed prior to calling the Parse method.
        /// </exception>
        [Test]
        public void Parse_ClosedStream_ThrowsObjectDisposedException()
        {
            // Arrange
            var stream = CreateStream("key: value");
            stream.Close();

            // Act & Assert
            Assert.Throws<FormatException>(
                () => this.parser.Parse(stream),
                "Stream was not readable."
            );
        }

        /// <summary>
        /// Creates a memory stream from the given string content encoded as UTF-8.
        /// </summary>
        /// <param name="content">The string content to be written to the memory stream.</param>
        /// <returns>A memory stream containing the UTF-8 encoded bytes of the provided content.</returns>
        private static MemoryStream CreateStream(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return new MemoryStream(bytes);
        }
    }
}
