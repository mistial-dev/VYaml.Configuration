// <copyright file="YamlParser.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Provides YAML parsing functionality for configuration using the VYaml library.
    /// </summary>
    public class YamlParser : IYamlParser
    {
        /// <inheritdoc/>
        public IDictionary<string, string?> Parse(Stream input)
        {
            return YamlConfigurationFileParser.Parse(input);
        }
    }
}
