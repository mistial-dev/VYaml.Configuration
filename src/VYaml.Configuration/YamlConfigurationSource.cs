// <copyright file="YamlConfigurationSource.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Represents a YAML file as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class YamlConfigurationSource : FileConfigurationSource
    {
        /// <summary>
        /// Builds the <see cref="YamlConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="YamlConfigurationProvider"/>.</returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            this.EnsureDefaults(builder);
            return new YamlConfigurationProvider(this);
        }

        /// <summary>
        /// Returns a string that represents this <see cref="YamlConfigurationSource"/>.
        /// </summary>
        /// <returns>A string representation of this source.</returns>
        public override string ToString()
        {
            return $"{this.GetType().Name} for '{this.Path}' (Optional: {this.Optional})";
        }
    }
}
