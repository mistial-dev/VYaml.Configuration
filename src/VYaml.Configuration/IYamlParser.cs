// <copyright file="IYamlParser.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Defines the contract for parsing YAML content into configuration key-value pairs.
    /// </summary>
    public interface IYamlParser
    {
        /// <summary>
        /// Parses YAML content from a stream into a dictionary of configuration key-value pairs.
        /// </summary>
        /// <param name="input">The stream containing YAML content to parse.</param>
        /// <returns>A dictionary containing the parsed configuration data with hierarchical keys.</returns>
        /// <exception cref="System.FormatException">Thrown when the YAML content is invalid or contains unsupported features.</exception>
        IDictionary<string, string?> Parse(Stream input);
    }
}
