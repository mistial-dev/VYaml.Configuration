// <copyright file="YamlConfigurationCrossPlatformTests.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

// ReSharper disable AccessToStaticMemberViaDerivedType
namespace VYaml.Configuration.Test
{
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Extensions.Configuration;
    using NUnit.Framework;
    using ClassicAssert = Assert;

    /// <summary>
    /// Cross-platform integration tests for YAML configuration.
    /// Tests file loading scenarios with real temporary files.
    /// </summary>
    [TestFixture]
    public class YamlConfigurationCrossPlatformTests
    {
        private string tempDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            this.tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(this.tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(this.tempDirectory))
            {
                Directory.Delete(this.tempDirectory, true);
            }
        }

        [Test]
        public void AddYamlFile_FileInTempDirectory_LoadsCorrectly()
        {
            // Arrange
            var yamlPath = Path.Combine(this.tempDirectory, "config.yaml");
            const string yamlContent = """

                app:
                  name: Test App
                  version: 1.0.0
                settings:
                  debug: true
                """;
            File.WriteAllText(yamlPath, yamlContent);

            // Act
            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile("config.yaml", optional: false, reloadOnChange: false)
                .Build();

            // Assert
            ClassicAssert.Multiple(() =>
            {
                ClassicAssert.That(configuration["app:name"], Is.EqualTo("Test App"));
                ClassicAssert.That(configuration["app:version"], Is.EqualTo("1.0.0"));
                ClassicAssert.That(configuration["settings:debug"], Is.EqualTo("true"));
            });
        }

        [Test]
        public void AddYamlFile_CrossPlatformPaths_LoadsCorrectly()
        {
            // Arrange
            var yamlPath = Path.Combine(this.tempDirectory, "paths.yaml");
            const string yamlContent = """

                paths:
                  windows: 'C:\Users\test\data'
                  unix: '/home/test/data'
                  relative: './data'
                  unc: '\\server\share\data'
                """;
            File.WriteAllText(yamlPath, yamlContent);

            // Act
            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile("paths.yaml", optional: false, reloadOnChange: false)
                .Build();

            // Assert
            ClassicAssert.Multiple(() =>
            {
                ClassicAssert.That(
                    configuration["paths:windows"],
                    Is.EqualTo(@"C:\Users\test\data")
                );
                ClassicAssert.That(configuration["paths:unix"], Is.EqualTo("/home/test/data"));
                ClassicAssert.That(configuration["paths:relative"], Is.EqualTo("./data"));
                ClassicAssert.That(configuration["paths:unc"], Is.EqualTo(@"\\server\share\data"));
            });
        }

        [Test]
        public void AddYamlFile_FileInSubdirectory_LoadsCorrectly()
        {
            // Arrange
            var subDir = Path.Combine(this.tempDirectory, "config", "features");
            Directory.CreateDirectory(subDir);
            var yamlPath = Path.Combine(subDir, "feature1.yaml");
            const string yamlContent = """

                feature:
                  enabled: true
                  name: subdirectory-test
                """;
            File.WriteAllText(yamlPath, yamlContent);

            // Act
            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.Combine("config", "features", "feature1.yaml"),
                    optional: false,
                    reloadOnChange: false
                )
                .Build();

            // Assert
            ClassicAssert.Multiple(() =>
            {
                ClassicAssert.That(configuration["feature:enabled"], Is.EqualTo("true"));
                ClassicAssert.That(configuration["feature:name"], Is.EqualTo("subdirectory-test"));
            });
        }

        [Test]
        public void AddYamlFile_OptionalFileNotFound_DoesNotThrow()
        {
            // Act & Assert - Should not throw for optional file
            ClassicAssert.DoesNotThrow(() =>
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(this.tempDirectory)
                    .AddYamlFile("missing.yaml", optional: true, reloadOnChange: false)
                    .Build();

                // Configuration should be empty but valid
                ClassicAssert.That(configuration.AsEnumerable().Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public void AddYamlFile_RequiredFileNotFound_ThrowsFileNotFoundException()
        {
            // Act & Assert
            ClassicAssert.Throws<FileNotFoundException>(() =>
            {
                _ = new ConfigurationBuilder()
                    .SetBasePath(this.tempDirectory)
                    .AddYamlFile("missing.yaml", optional: false, reloadOnChange: false)
                    .Build();
            });
        }

        [Test]
        public void AddYamlFile_FileWithBOM_LoadsCorrectly()
        {
            // Arrange
            var yamlPath = Path.Combine(this.tempDirectory, "bom.yaml");
            const string yamlContent = "key: value with BOM";

            // Write with UTF-8 BOM
            using (var writer = new StreamWriter(yamlPath, false, new UTF8Encoding(true)))
            {
                writer.Write(yamlContent);
            }

            // Act
            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile("bom.yaml", optional: false, reloadOnChange: false)
                .Build();

            // Assert
            ClassicAssert.That(configuration["key"], Is.EqualTo("value with BOM"));
        }

        [Test]
        public void AddYamlFile_EmptyFile_LoadsAsEmptyConfiguration()
        {
            // Arrange
            var yamlPath = Path.Combine(this.tempDirectory, "empty.yaml");
            File.WriteAllText(yamlPath, string.Empty);

            // Act
            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile("empty.yaml", optional: false, reloadOnChange: false)
                .Build();

            // Assert
            ClassicAssert.That(configuration.AsEnumerable().Count(), Is.EqualTo(0));
        }

        [Test]
        public void AddYamlFile_FileWithOnlyComments_LoadsAsEmptyConfiguration()
        {
            // Arrange
            var yamlPath = Path.Combine(this.tempDirectory, "comments.yaml");
            const string yamlContent = """

                # This is a comment
                # Another comment
                # Yet another comment
                """;
            File.WriteAllText(yamlPath, yamlContent);

            // Act
            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile("comments.yaml", optional: false, reloadOnChange: false)
                .Build();

            // Assert
            ClassicAssert.That(configuration.AsEnumerable().Count(), Is.EqualTo(0));
        }

        [Test]
        [Platform(Include = "Win")]
        public void AddYamlFile_WindowsSpecificPath_LoadsCorrectly()
        {
            // This test only runs on Windows
            // Tests handling of drive letters and backslashes
            var yamlPath = Path.Combine(this.tempDirectory, "windows.yaml");
            const string yamlContent = "test: windows specific";
            File.WriteAllText(yamlPath, yamlContent);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile("windows.yaml", optional: false, reloadOnChange: false)
                .Build();

            ClassicAssert.That(configuration["test"], Is.EqualTo("windows specific"));
        }

        [Test]
        [Platform(Exclude = "Win")]
        public void AddYamlFile_UnixSpecificPath_LoadsCorrectly()
        {
            // This test only runs on Unix-like systems
            // Tests handling of forward slashes
            var yamlPath = Path.Combine(this.tempDirectory, "unix.yaml");
            const string yamlContent = "test: unix specific";
            File.WriteAllText(yamlPath, yamlContent);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile("unix.yaml", optional: false, reloadOnChange: false)
                .Build();

            ClassicAssert.That(configuration["test"], Is.EqualTo("unix specific"));
        }
    }
}
