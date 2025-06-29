// <copyright file="YamlConfigurationFileParser.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Extensions.Configuration;
    using VYaml.Parser;

    /// <summary>
    /// Provides parsing of YAML files into configuration key-value pairs.
    /// </summary>
    internal sealed class YamlConfigurationFileParser
    {
        private readonly Dictionary<string, string?> data = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Represents a stack of paths currently being traversed or visited within the YAML configuration.
        /// Used for constructing hierarchical keys during the parsing of YAML data.
        /// </summary>
        private readonly Stack<string> paths = new();

        /// <summary>
        /// Provides functionality for parsing YAML configuration files into key-value pairs.
        /// </summary>
        private YamlConfigurationFileParser() { }

        /// <summary>
        /// Parses a YAML stream into configuration key-value pairs.
        /// </summary>
        /// <param name="input">The stream containing YAML content.</param>
        /// <returns>A dictionary of configuration key-value pairs.</returns>
        public static IDictionary<string, string?> Parse(Stream input) =>
            new YamlConfigurationFileParser().ParseStream(input);

        /// <summary>
        /// Reads and parses a YAML input stream into a dictionary of configuration key-value pairs.
        /// </summary>
        /// <param name="input">The stream containing YAML content to parse.</param>
        /// <returns>A dictionary containing configuration key-value pairs extracted from the YAML stream.</returns>
        private Dictionary<string, string?> ParseStream(Stream input)
        {
            try
            {
                using var streamReader = new StreamReader(
                    input,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 1024,
                    leaveOpen: true
                );
                var yaml = streamReader.ReadToEnd();

                CheckForTabCharacters(yaml);

                if (string.IsNullOrWhiteSpace(yaml))
                {
                    return this.data;
                }

                var utf8Bytes = Encoding.UTF8.GetBytes(yaml);
                var parser = Parser.YamlParser.FromBytes(utf8Bytes);

                // Skip to the document content
                parser.SkipAfter(ParseEventType.StreamStart);

                while (parser.Read())
                {
                    if (parser.CurrentEventType == ParseEventType.DocumentStart)
                    {
                        continue;
                    }

                    if (
                        parser.CurrentEventType == ParseEventType.DocumentEnd
                        || parser.CurrentEventType == ParseEventType.StreamEnd
                    )
                    {
                        break;
                    }

                    this.VisitValue(ref parser);
                    break;
                }
            }
            catch (YamlParserException ex)
            {
                throw new FormatException($"YAML parsing error: {ex.Message}", ex);
            }
            catch (FormatException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FormatException($"Failed to parse YAML configuration: {ex.Message}", ex);
            }

            return this.data;
        }

        /// <summary>
        /// Verifies that the specified YAML content does not contain tab characters, as they are not allowed for indentation in YAML.
        /// </summary>
        /// <param name="yaml">The YAML content to validate.</param>
        /// <exception cref="FormatException">
        /// Thrown when one or more tab characters are detected in the YAML content. The exception includes the line number with the offending tab character.
        /// </exception>
        private static void CheckForTabCharacters(string yaml)
        {
            // Check for tab characters
            if (!yaml.Contains("\t"))
            {
                return;
            }

            var lines = yaml.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("\t"))
                {
                    throw new FormatException(
                        $"YAML configuration error: Tab characters are not allowed. Use spaces for indentation. Found tab at line {i + 1}"
                    );
                }
            }
        }

        /// <summary>
        /// Processes the current YAML parse event and delegates further processing
        /// based on the event type (e.g., scalar, sequence, mapping).
        /// </summary>
        /// <param name="parser">The YAML parser that provides the current parse event and context.</param>
        private void VisitValue(ref Parser.YamlParser parser)
        {
            switch (parser.CurrentEventType)
            {
                case ParseEventType.Scalar:
                    this.VisitScalar(ref parser);
                    break;

                case ParseEventType.SequenceStart:
                    this.VisitSequence(ref parser);
                    break;

                case ParseEventType.MappingStart:
                    this.VisitMapping(ref parser);
                    break;

                case ParseEventType.Alias:
                    throw new FormatException(
                        $"YAML aliases are not supported in configuration files at line {parser.CurrentMark.Line}"
                    );

                case ParseEventType.DocumentEnd:
                case ParseEventType.StreamEnd:
                    break;

                case ParseEventType.Nothing:
                case ParseEventType.StreamStart:
                case ParseEventType.DocumentStart:
                case ParseEventType.SequenceEnd:
                case ParseEventType.MappingEnd:
                default:
                    throw new FormatException(
                        $"Unexpected YAML token '{parser.CurrentEventType}' at line {parser.CurrentMark.Line}"
                    );
            }
        }

        /// <summary>
        /// Processes a scalar value from the YAML parser and adds it to the configuration data dictionary.
        /// </summary>
        /// <param name="parser">The instance of <see cref="Parser.YamlParser"/> currently traversing the YAML structure.</param>
        private void VisitScalar(ref Parser.YamlParser parser)
        {
            if (this.paths.Count <= 0)
            {
                return;
            }

            var key = this.paths.Peek();
            var value = parser.GetScalarAsString();

            // Handle null values properly
            if (value == "null" || value == "Null" || value == "NULL" || value == "~")
            {
                this.data[key] = null;
            }
            else
            {
                this.data[key] = value;
            }
        }

        /// <summary>
        /// Processes a YAML sequence node by traversing its elements
        /// and handling nested structures or values within the sequence.
        /// </summary>
        /// <param name="parser">The <see cref="Parser.YamlParser"/> instance used to parse the current YAML document.</param>
        private void VisitSequence(ref Parser.YamlParser parser)
        {
            int index = 0;

            while (parser.Read() && parser.CurrentEventType != ParseEventType.SequenceEnd)
            {
                this.EnterContext(index.ToString());
                this.VisitValue(ref parser);
                this.ExitContext();
                index++;
            }

            this.SetEmptyIfElementIsEmpty(isEmpty: index == 0);
        }

        /// <summary>
        /// Processes a YAML mapping structure and parses its key-value pairs.
        /// </summary>
        /// <param name="parser">
        /// The YAML parser used to read and navigate the mapping structure.
        /// </param>
        private void VisitMapping(ref Parser.YamlParser parser)
        {
            var isEmpty = true;

            while (parser.Read() && parser.CurrentEventType != ParseEventType.MappingEnd)
            {
                isEmpty = false;

                // Read the key
                if (parser.CurrentEventType != ParseEventType.Scalar)
                {
                    throw new FormatException(
                        $"Expected scalar key in YAML mapping at line {parser.CurrentMark.Line}"
                    );
                }

                var key = parser.GetScalarAsString();

                if (string.IsNullOrEmpty(key))
                {
                    throw new FormatException(
                        $"YAML mapping key cannot be empty at line {parser.CurrentMark.Line}"
                    );
                }

                this.EnterContext(key!);

                // Read the value
                if (!parser.Read())
                {
                    throw new FormatException($"Unexpected end of YAML stream after key '{key}'");
                }

                this.VisitValue(ref parser);
                this.ExitContext();
            }

            this.SetNullIfElementIsEmpty(isEmpty);
        }

        /// <summary>
        /// Adds a context to the stack of paths being traversed during YAML parsing.
        /// This method is used to build hierarchical keys based on the current traversal path.
        /// </summary>
        /// <param name="context">The context or key to be added to the current traversal path.</param>
        private void EnterContext(string context) =>
            this.paths.Push(
                this.paths.Count > 0
                    ? this.paths.Peek() + ConfigurationPath.KeyDelimiter + context
                    : context
            );

        /// <summary>
        /// Exits the current context by removing the most recently entered path from the stack of paths being traversed.
        /// Used to maintain accurate hierarchical context while parsing YAML data.
        /// </summary>
        private void ExitContext() => this.paths.Pop();

        /// <summary>
        /// Sets the value of the current configuration key to null if the corresponding YAML element is empty.
        /// </summary>
        /// <param name="isEmpty">Indicates whether the YAML element being processed is empty.</param>
        private void SetNullIfElementIsEmpty(bool isEmpty)
        {
            if (isEmpty && this.paths.Count > 0)
            {
                this.data[this.paths.Peek()] = null;
            }
        }

        /// <summary>
        /// Sets an empty string as a value for the current hierarchical key if the specified condition indicates the element is empty.
        /// </summary>
        /// <param name="isEmpty">Indicates whether the current element is empty.</param>
        private void SetEmptyIfElementIsEmpty(bool isEmpty)
        {
            if (isEmpty && this.paths.Count > 0)
            {
                this.data[this.paths.Peek()] = string.Empty;
            }
        }
    }
}
