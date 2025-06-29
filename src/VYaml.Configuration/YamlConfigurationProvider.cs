// <copyright file="YamlConfigurationProvider.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// A YAML file based <see cref="FileConfigurationProvider"/>.
    /// </summary>
    public class YamlConfigurationProvider : FileConfigurationProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="YamlConfigurationProvider"/> class.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public YamlConfigurationProvider(YamlConfigurationSource source)
            : base(source) { }

        /// <summary>
        /// Loads the YAML data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <exception cref="FormatException">Thrown when the YAML content is invalid.</exception>
        public override void Load(Stream stream)
        {
            this.Data = YamlConfigurationFileParser.Parse(stream);
        }
    }
}
