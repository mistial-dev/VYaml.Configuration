// <copyright file="YamlConfigurationExtensions.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.FileProviders;

    /// <summary>
    /// Extension methods for adding <see cref="YamlConfigurationProvider"/>.
    /// </summary>
    public static class YamlConfigurationExtensions
    {
        /// <summary>
        /// Adds a YAML configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public static IConfigurationBuilder AddYamlFile(
            this IConfigurationBuilder builder,
            string path
        )
        {
            return AddYamlFile(
                builder,
                provider: null,
                path: path,
                optional: false,
                reloadOnChange: false
            );
        }

        /// <summary>
        /// Adds a YAML configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public static IConfigurationBuilder AddYamlFile(
            this IConfigurationBuilder builder,
            string path,
            bool optional
        )
        {
            return AddYamlFile(
                builder,
                provider: null,
                path: path,
                optional: optional,
                reloadOnChange: false
            );
        }

        /// <summary>
        /// Adds a YAML configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public static IConfigurationBuilder AddYamlFile(
            this IConfigurationBuilder builder,
            string path,
            bool optional,
            bool reloadOnChange
        )
        {
            return AddYamlFile(
                builder,
                provider: null,
                path: path,
                optional: optional,
                reloadOnChange: reloadOnChange
            );
        }

        /// <summary>
        /// Adds a YAML configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="provider">The <see cref="IFileProvider"/> to use to access the file.</param>
        /// <param name="path">Path relative to the base path stored in
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public static IConfigurationBuilder AddYamlFile(
            this IConfigurationBuilder builder,
            IFileProvider? provider,
            string path,
            bool optional,
            bool reloadOnChange
        )
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("File path must be a non-empty string.", nameof(path));
            }

            var source = new YamlConfigurationSource
            {
                FileProvider = provider,
                Path = path,
                Optional = optional,
                ReloadOnChange = reloadOnChange,
            };
            if (reloadOnChange)
            {
                // Speed up reload-on-change by removing debounce delay (tests and scenarios expect timely reload)
                source.ReloadDelay = 0;
            }

            return builder.Add(source);
        }

        /// <summary>
        /// Adds a YAML configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="configureSource">Configures the source.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public static IConfigurationBuilder AddYamlFile(
            this IConfigurationBuilder builder,
            Action<YamlConfigurationSource>? configureSource
        )
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var source = new YamlConfigurationSource();
            configureSource?.Invoke(source);
            return builder.Add(source);
        }

        /// <summary>
        /// Adds a YAML configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="stream">The <see cref="Stream"/> to read the yaml configuration data from.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="stream"/> is null.</exception>
        public static IConfigurationBuilder AddYamlStream(
            this IConfigurationBuilder builder,
            Stream stream
        )
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var source = new YamlStreamConfigurationSource { Stream = stream };
            return builder.Add(source);
        }
    }
}
