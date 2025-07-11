﻿// <copyright file="YamlStreamConfigurationSource.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Represents a YAML stream as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class YamlStreamConfigurationSource : StreamConfigurationSource
    {
        /// <summary>
        /// Builds the <see cref="YamlStreamConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="YamlStreamConfigurationProvider"/>.</returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new YamlStreamConfigurationProvider(this);
        }
    }
}
