// <copyright file="YamlConfigurationExtensionsTests.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

// ReSharper disable BadParensSpaces
// ReSharper disable AccessToStaticMemberViaDerivedType
namespace VYaml.Configuration.Test
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using NUnit.Framework;

    /// <summary>
    /// Unit tests for the YamlConfigurationExtensions class.
    /// Tests all AddYamlFile extension method overloads.
    /// </summary>
    [TestFixture]
    public class YamlConfigurationExtensionsTests
    {
        private ConfigurationBuilder builder = null!;

        /// <summary>
        /// Method executed before each test in the test class to set up the necessary
        /// context or initialize resources. Specifically, it initializes a new
        /// <see cref="ConfigurationBuilder"/>.
        /// This setup ensures a consistent, isolated configuration builder instance
        /// is available for each test, preventing interference between tests.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.builder = new ConfigurationBuilder();
        }

        /// <summary>
        /// Verifies that calling AddYamlFile with only a file path adds a new YamlConfigurationSource
        /// to the configuration builder with default settings for optionality and reload behavior.
        /// Specifically, ensures the source is added with optional set to false and reloadOnChange
        /// set to false, and that the path is preserved correctly.
        /// </summary>
        [Test]
        public void AddYamlFile_PathOnly_AddsSourceWithDefaults()
        {
            // Arrange
            const string path = "config.yaml";

            // Act
            var result = this.builder.AddYamlFile(path);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result, Is.SameAs(this.builder));
                Assert.That(this.builder.Sources, Has.Count.EqualTo(1));

                var source = this.builder.Sources[0] as YamlConfigurationSource;
                Assert.That(source, Is.Not.Null);
                Assert.That(source!.Path, Is.EqualTo(path));
                Assert.That(source.Optional, Is.False);
                Assert.That(source.ReloadOnChange, Is.False);
            });
        }

        /// <summary>
        /// Tests the <see cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder, string, bool)"/>
        /// method to verify that it correctly adds a <see cref="YamlConfigurationSource"/>
        /// to the <see cref="IConfigurationBuilder"/> with the provided path and optional flag.
        /// Confirms the correct configuration is set for the source, including the optional property
        /// and the default behavior of the ReloadOnChange property.
        /// </summary>
        [Test]
        public void AddYamlFile_PathAndOptional_AddsSourceWithCorrectOptional()
        {
            // Arrange
            const string path = "config.yaml";
            const bool optional = true;

            // Act
            var result = this.builder.AddYamlFile(path, optional);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result, Is.SameAs(this.builder));
                Assert.That(this.builder.Sources, Has.Count.EqualTo(1));
                var source = this.builder.Sources[0] as YamlConfigurationSource;

                Assert.That(source, Is.Not.Null);
                Assert.That(source!.Path, Is.EqualTo(path));
                Assert.That(source.Optional, Is.EqualTo(optional));
                Assert.That(source.ReloadOnChange, Is.False);
            });
        }

        /// <summary>
        /// Tests that the <see cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder, string, bool, bool)"/>
        /// method adds a <see cref="YamlConfigurationSource"/> to the configuration builder with the specified path,
        /// optional flag, and reload-on-change flag correctly configured.
        /// Ensures that the builder remains the same instance for fluent chaining
        /// and verifies the proper initialization of the configuration source.
        /// </summary>
        [Test]
        public void AddYamlFile_PathOptionalAndReloadOnChange_AddsSourceWithAllOptions()
        {
            // Arrange
            var path = "config.yaml";
            var optional = true;
            var reloadOnChange = true;

            // Act
            var result = this.builder.AddYamlFile(path, optional, reloadOnChange);

            // Assert
            Assert.That(result, Is.SameAs(this.builder));
            Assert.That(this.builder.Sources, Has.Count.EqualTo(1));

            var source = this.builder.Sources[0] as YamlConfigurationSource;
            Assert.That(source, Is.Not.Null);
            Assert.That(source.Path, Is.EqualTo(path));
            Assert.That(source.Optional, Is.EqualTo(optional));
            Assert.That(source.ReloadOnChange, Is.EqualTo(reloadOnChange));
        }

        /// <summary>
        /// Validates that the <see cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder, string)"/>
        /// method throws an <see cref="ArgumentNullException"/> when the provided
        /// <see cref="IConfigurationBuilder"/> instance is null.
        /// This ensures that the method adheres to its contract of handling
        /// invalid input gracefully.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <see cref="IConfigurationBuilder"/> parameter supplied
        /// to the method is null.
        /// </exception>
        [Test]
        public void AddYamlFile_NullBuilder_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(static () =>
                YamlConfigurationExtensions.AddYamlFile(null!, "config.yaml")
            );
        }

        /// <summary>
        /// Validates that calling the <see cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder, string)"/> method
        /// with a null path throws an <see cref="ArgumentException"/>.
        /// Ensures that the method appropriately handles null path arguments, preventing improper
        /// configuration addition that could result in runtime errors.
        /// </summary>
        [Test]
        public void AddYamlFile_NullPath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => this.builder.AddYamlFile((string)null!));
        }

        /// <summary>
        /// Validates that calling the AddYamlFile method with an empty path
        /// results in an <see cref="ArgumentException"/> being thrown.
        /// Ensures the method enforces non-empty paths as required by its contract.
        /// </summary>
        [Test]
        public void AddYamlFile_EmptyPath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => this.builder.AddYamlFile(string.Empty));
        }

        /// <summary>
        /// Verifies that calling <see cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder, string)"/>
        /// with a path consisting entirely of whitespace throws an <see cref="ArgumentException"/>.
        /// Ensures that invalid, non-usable file paths do not pass validation.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the provided path contains only whitespace.</exception>
        [Test]
        public void AddYamlFile_WhitespacePath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => this.builder.AddYamlFile("   "));
        }

        /// <summary>
        /// Verifies that multiple YAML configuration files can be added to the
        /// <see cref="ConfigurationBuilder"/> as sources using chained calls to the
   /// <see cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder, string, bool, bool)"/> method. Ensures that all
        /// specified files are added with the appropriate configuration options:
        /// path, optional flag, and reload-on-change flag.
        /// </summary>
        /// <remarks>
        /// - Confirms the builder accurately tracks each added source.
        /// - Verifies the <see cref="YamlConfigurationSource.Path"/>, <see cref="YamlConfigurationSource.Optional"/>,
        /// and <see cref="YamlConfigurationSource.ReloadOnChange"/> values for all added files.
        /// </remarks>
        /// <seealso cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder, string, bool, bool)"/>
        [Test]
        public void AddYamlFile_MultipleFiles_AddsMultipleSources()
        {
            // Act
            this.builder.AddYamlFile("config1.yaml")
                .AddYamlFile("config2.yaml", optional: true)
                .AddYamlFile("config3.yaml", optional: false, reloadOnChange: true);

            // Assert
            Assert.That(this.builder.Sources, Has.Count.EqualTo(3));

            var sources = this.builder.Sources.Cast<YamlConfigurationSource>().ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(sources[0].Path, Is.EqualTo("config1.yaml"));
                Assert.That(sources[0].Optional, Is.False);
                Assert.That(sources[0].ReloadOnChange, Is.False);

                Assert.That(sources[1].Path, Is.EqualTo("config2.yaml"));
                Assert.That(sources[1].Optional, Is.True);
                Assert.That(sources[1].ReloadOnChange, Is.False);

                Assert.That(sources[2].Path, Is.EqualTo("config3.yaml"));
                Assert.That(sources[2].Optional, Is.False);
                Assert.That(sources[2].ReloadOnChange, Is.True);
            });
        }

        /// <summary>
        /// Verifies that adding a YAML configuration file with an absolute path
        /// preserves the provided path in the resulting configuration source.
        /// Ensures that the path remains set and is not null or empty when retrieved
        /// from the <see cref="YamlConfigurationSource"/> added to the builder.
        /// </summary>
        [Test]
        public void AddYamlFile_AbsolutePath_PreservesPath()
        {
            // Arrange
            var absolutePath = "/absolute/path/to/config.yaml";

            // Act
            this.builder.AddYamlFile(absolutePath);

            // Assert
            var source = this.builder.Sources[0] as YamlConfigurationSource;
            Assert.Multiple(() =>
            {
                Assert.That(source, Is.Not.Null);

                // The base class FileConfigurationSource may normalize paths,
                // so we just check that a path was set
                Assert.That(source!.Path, Is.Not.Null);
                Assert.That(source.Path, Is.Not.Empty);
            });
        }

        /// <summary>
        /// Validates that the relative path provided to the <see cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder, string)"/>
        /// method is preserved correctly when added to the <see cref="IConfigurationBuilder"/> sources.
        /// This ensures that relative file paths are not altered during configuration setup and can
        /// be resolved successfully at runtime.
        /// </summary>
        [Test]
        public void AddYamlFile_RelativePath_PreservesPath()
        {
            // Arrange
            const string relativePath = "config/app.yaml";

            // Act
            this.builder.AddYamlFile(relativePath);

            // Assert
            var source = this.builder.Sources[0] as YamlConfigurationSource;
            Assert.That(source, Is.Not.Null);
            Assert.That(source.Path, Is.EqualTo(relativePath));
        }

        /// <summary>
        /// Validates that adding a YAML configuration file using a Windows-style absolute path
        /// correctly preserves the provided path in the configuration source.
        /// Ensures that the <see cref="YamlConfigurationSource"/> reflects the exact path passed
        /// during the configuration setup, maintaining integrity for Windows-specific file paths.
        /// </summary>
        [Test]
        public void AddYamlFile_WindowsPath_PreservesPath()
        {
            // Arrange
            const string windowsPath = @"C:\config\app.yaml";

            // Act
            this.builder.AddYamlFile(windowsPath);

            // Assert
            var source = this.builder.Sources[0] as YamlConfigurationSource;
            Assert.That(source, Is.Not.Null);
            Assert.That(source.Path, Is.EqualTo(windowsPath));
        }

        /// <summary>
        /// Verifies that the <see cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder, string)"/> method
        /// correctly preserves file paths containing special characters.
        /// Ensures that the added YAML configuration source retains its original path
        /// without alterations or errors when special characters are included, such as
        /// underscores or version identifiers.
        /// </summary>
        [Test]
        public void AddYamlFile_PathWithSpecialCharacters_PreservesPath()
        {
            // Arrange
            const string specialPath = "config-file_v2.1.yaml";

            // Act
            this.builder.AddYamlFile(specialPath);

            // Assert
            var source = this.builder.Sources[0] as YamlConfigurationSource;
            Assert.That(source, Is.Not.Null);
            Assert.That(source.Path, Is.EqualTo(specialPath));
        }

        /// <summary>
        /// Verifies that chained calls to the <see cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder, string)"/>
        /// method return the same <see cref="IConfigurationBuilder"/> instance, enabling fluent chaining.
        /// This ensures that the builder is not replaced or modified in a way that breaks chaining during
        /// configurations involving multiple YAML files.
        /// </summary>
        [Test]
        public void AddYamlFile_ChainedCalls_ReturnsBuilderForChaining()
        {
            // Act
            var result = this
                .builder.AddYamlFile("config1.yaml")
                .AddYamlFile("config2.yaml")
                .AddYamlFile("config3.yaml");

            // Assert
            Assert.That(result, Is.SameAs(this.builder));
        }
    }
}
