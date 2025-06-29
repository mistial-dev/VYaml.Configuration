// <copyright file="YamlConfigurationSourceTests.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

// ReSharper disable BadParensSpaces
// ReSharper disable AccessToStaticMemberViaDerivedType
namespace VYaml.Configuration.Test
{
    using System;
    using Microsoft.Extensions.Configuration;
    using NUnit.Framework;

    /// <summary>
    /// Contains unit tests for the YamlConfigurationSource class.
    /// Verifies the behavior and functionality of the configuration source,
    /// including properties, builder interactions, and default settings.
    /// </summary>
    [TestFixture]
    public class YamlConfigurationSourceTests
    {
        /// <summary>
        /// Represents the YAML configuration source used in tests.
        /// Its properties and functionality are initialized and tested within the YamlConfigurationSourceTests.
        /// </summary>
        private YamlConfigurationSource source = null!;

        /// <summary>
        /// Represents a configuration builder instance used to construct and configure
        /// key-value pair settings for an application. Acts as an entry point for adding
        /// various configuration sources and enables chained configuration setup.
        /// </summary>
        private ConfigurationBuilder builder = null!;

        /// <summary>
        /// Initializes the test environment for the unit tests in the <see cref="YamlConfigurationSourceTests"/> class.
        /// Sets up the <see cref="ConfigurationBuilder"/> and configures the <see cref="YamlConfigurationSource"/> with default test values.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.builder = new ConfigurationBuilder();

            this.source = new YamlConfigurationSource
            {
                Path = "/test/config.yaml",
                Optional = false,
                ReloadOnChange = false,
            };
        }

        /// <summary>
        /// Tests that the <see cref="YamlConfigurationSource.Build(IConfigurationBuilder)"/> method
        /// returns a non-null instance of <see cref="YamlConfigurationProvider"/> when built
        /// with a valid <see cref="IConfigurationBuilder"/> instance.
        /// </summary>
        [Test]
        public void Build_ValidSource_ReturnsYamlConfigurationProvider()
        {
            // Act
            var provider = this.source.Build(this.builder);

            // Assert
            Assert.That(provider, Is.Not.Null);
            Assert.That(provider, Is.InstanceOf<YamlConfigurationProvider>());
        }

        /// <summary>
        /// Verifies that calling the <see cref="YamlConfigurationSource.Build"/> method ensures default values are set.
        /// Specifically, it tests that the <see cref="YamlConfigurationSource.FileProvider"/> property
        /// is assigned a default value if it is initially null.
        /// </summary>
        [Test]
        public void Build_EnsuresDefaults_SetsDefaultFileProvider()
        {
            // Arrange
            this.source.FileProvider = null;

            // Act
            var provider = this.source.Build(this.builder);

            // Assert
            Assert.That(provider, Is.Not.Null);
            Assert.That(this.source.FileProvider, Is.Not.Null);
        }

        // Removed test that used Moq - FileProvider is now handled by base class

        /// <summary>
        /// Tests that the <see cref="YamlConfigurationSource.Build"/> method throws an
        /// <see cref="ArgumentNullException"/> when called with a null <see cref="IConfigurationBuilder"/> argument.
        /// </summary>
        [Test]
        public void Build_NullBuilder_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => this.source.Build(null!));
        }

        /// <summary>
        /// Verifies that the <see cref="YamlConfigurationSource.Path"/> property of the <see cref="YamlConfigurationSource"/> class
        /// can be correctly set and retrieved.
        /// </summary>
        [Test]
        public void Path_GetSet_WorksCorrectly()
        {
            // Arrange
            var testPath = "/custom/path/config.yaml";

            // Act
            this.source.Path = testPath;

            // Assert
            Assert.That(this.source.Path, Is.EqualTo(testPath));
        }

        /// <summary>
        /// Verifies that the <see cref="YamlConfigurationSource.Optional"/> property of the <see cref="YamlConfigurationSource"/> class
        /// can be correctly retrieved and updated.
        /// </summary>
        [Test]
        public void Optional_GetSet_WorksCorrectly()
        {
            // Act & Assert - default
            Assert.That(this.source.Optional, Is.False);

            // Act & Assert - set to true
            this.source.Optional = true;
            Assert.That(this.source.Optional, Is.True);

            // Act & Assert - set to false
            this.source.Optional = false;
            Assert.That(this.source.Optional, Is.False);
        }

        /// <summary>
        /// Verifies that the <see cref="YamlConfigurationSource.ReloadOnChange"/> property can be correctly set and retrieved.
        /// Ensures that the property maintains its assigned state across changes.
        /// </summary>
        [Test]
        public void ReloadOnChange_GetSet_WorksCorrectly()
        {
            // Act & Assert - default
            Assert.That(this.source.ReloadOnChange, Is.False);

            // Act & Assert - set to true
            this.source.ReloadOnChange = true;
            Assert.That(this.source.ReloadOnChange, Is.True);

            // Act & Assert - set to false
            this.source.ReloadOnChange = false;
            Assert.That(this.source.ReloadOnChange, Is.False);
        }

        /// <summary>
        /// Verifies that the default values of a newly constructed <see cref="YamlConfigurationSource"/> instance
        /// are set correctly. Ensures that the properties <c>Path</c>, <c>Optional</c>, <c>ReloadOnChange</c>,
        /// and <c>FileProvider</c> have the expected initial values.
        /// </summary>
        [Test]
        public void Constructor_DefaultValues_SetCorrectly()
        {
            // Arrange & Act
            var configSource = new YamlConfigurationSource();

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(configSource.Path, Is.Null);
                Assert.That(configSource.Optional, Is.False);
                Assert.That(configSource.ReloadOnChange, Is.False);
                Assert.That(configSource.FileProvider, Is.Null);
            });
        }

        /// <summary>
        /// Verifies that calling the <see cref="YamlConfigurationSource.Build"/> method multiple times
        /// with the same <see cref="IConfigurationBuilder"/> instance produces distinct instances of
        /// <see cref="YamlConfigurationProvider"/>.
        /// </summary>
        [Test]
        public void Build_MultipleCallsSameBuilder_ReturnsDifferentProviders()
        {
            // Act
            var provider1 = this.source.Build(this.builder);
            var provider2 = this.source.Build(this.builder);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(provider1, Is.Not.Null);
                Assert.That(provider2, Is.Not.Null);
                Assert.That(provider1, Is.Not.SameAs(provider2));
                Assert.That(provider1, Is.InstanceOf<YamlConfigurationProvider>());
                Assert.That(provider2, Is.InstanceOf<YamlConfigurationProvider>());
            });
        }

        /// <summary>
        /// Verifies that calling the <see cref="YamlConfigurationSource.Build"/> method with different
        /// <see cref="IConfigurationBuilder"/> instances returns distinct and valid <see cref="YamlConfigurationProvider"/> objects.
        /// </summary>
        [Test]
        public void Build_DifferentBuilders_ReturnsValidProviders()
        {
            // Arrange
            var builder1 = new ConfigurationBuilder();
            var builder2 = new ConfigurationBuilder();

            // Act
            var provider1 = this.source.Build(builder1);
            var provider2 = this.source.Build(builder2);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(provider1, Is.Not.Null);
                Assert.That(provider2, Is.Not.Null);
                Assert.That(provider1, Is.InstanceOf<YamlConfigurationProvider>());
                Assert.That(provider2, Is.InstanceOf<YamlConfigurationProvider>());
            });
        }

        /// <summary>
        /// Verifies that the <see cref="YamlConfigurationSource.ToString"/> method
        /// correctly returns a descriptive string containing details about the configuration source.
        /// </summary>
        [Test]
        public void ToString_ReturnsDescriptiveString()
        {
            // Act
            var description = this.source.ToString();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(description, Is.Not.Null);
                Assert.That(description, Does.Contain("YamlConfigurationSource"));
                Assert.That(description, Does.Contain(this.source.Path));
            });
        }
    }
}
