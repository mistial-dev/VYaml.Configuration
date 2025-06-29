// <copyright file="YamlConfigurationProviderTests.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

// ReSharper disable BadParensSpaces
// ReSharper disable AccessToStaticMemberViaDerivedType
namespace VYaml.Configuration.Test
{
    using System;
    using System.IO;
    using System.Text;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Primitives;
    using NUnit.Framework;

    /// <summary>
    /// Unit tests for the YamlConfigurationProvider class.
    /// Tests provider functionality with in-memory streams and error handling.
    /// </summary>
    [TestFixture]
    public class YamlConfigurationProviderTests
    {
        [Test]
        public void Load_ValidYamlStream_PopulatesData()
        {
            // Arrange
            const string yaml = """

                key1: value1
                key2: value2
                nested:
                  key3: value3
                  key4: value4
                """;
            var source = new YamlConfigurationSource
            {
                Path = "test.yaml",
                FileProvider = new TestStreamFileProvider(yaml),
            };
            var provider = new YamlConfigurationProvider(source);

            // Act
            provider.Load();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(provider.TryGet("key1", out var value1), Is.True);
                Assert.That(value1, Is.EqualTo("value1"));

                Assert.That(provider.TryGet("key2", out var value2), Is.True);
                Assert.That(value2, Is.EqualTo("value2"));

                Assert.That(provider.TryGet("nested:key3", out var value3), Is.True);
                Assert.That(value3, Is.EqualTo("value3"));

                Assert.That(provider.TryGet("nested:key4", out var value4), Is.True);
                Assert.That(value4, Is.EqualTo("value4"));
            });
        }

        /// <summary>
        /// Verifies that the YAML configuration provider correctly handles an empty YAML stream
        /// by producing an empty data set. This test ensures that the provider does not throw exceptions
        /// or produce unintended results when no content is provided in the stream.
        /// </summary>
        [Test]
        public void Load_EmptyYamlStream_CreatesEmptyData()
        {
            // Arrange
            var source = new YamlConfigurationSource
            {
                Path = "empty.yaml",
                FileProvider = new TestStreamFileProvider(string.Empty),
            };
            var provider = new YamlConfigurationProvider(source);

            // Act
            provider.Load();

            // Assert
            Assert.That(provider.TryGet("anykey", out _), Is.False);
        }

        /// <summary>
        /// Verifies that the YAML configuration provider throws a FormatException
        /// when attempting to load an invalid YAML content. This test ensures that
        /// malformed indentation or other structural issues in the document are correctly
        /// detected and result in an appropriate error.
        /// </summary>
        [Test]
        public void Load_InvalidYaml_ThrowsFormatException()
        {
            // Arrange
            const string invalidYaml = """

                key1: value1
                  invalid: indentation
                key2: value2
                """;
            var source = new YamlConfigurationSource
            {
                Path = "invalid.yaml",
                FileProvider = new TestStreamFileProvider(invalidYaml),
            };
            var provider = new YamlConfigurationProvider(source);

            // Act & Assert
            var ex = Assert.Throws<InvalidDataException>(() => provider.Load());
            Assert.That(ex.InnerException, Is.TypeOf<FormatException>());
            Assert.That(ex.InnerException!.Message, Does.Contain("Failed to parse YAML"));
        }

        /// <summary>
        /// Ensures that the YAML configuration provider throws a FormatException
        /// when attempting to load YAML content containing tab characters, as tab
        /// characters are not valid in YAML formatting.
        /// </summary>
        [Test]
        public void Load_YamlWithTabs_ThrowsFormatException()
        {
            // Arrange
            const string yamlWithTabs = "key:\tvalue"; // Tab character
            var source = new YamlConfigurationSource
            {
                Path = "tabs.yaml",
                FileProvider = new TestStreamFileProvider(yamlWithTabs),
            };
            var provider = new YamlConfigurationProvider(source);

            // Act & Assert
            var ex = Assert.Throws<InvalidDataException>(() => provider.Load());
            Assert.That(ex.InnerException, Is.TypeOf<FormatException>());
            Assert.That(ex.InnerException!.Message, Does.Contain("Tab").Or.Contain("tab"));
        }

        /// <summary>
        /// Validates that the YAML configuration provider correctly processes YAML files containing null values,
        /// and ensures null values are properly represented and retrievable using keys.
        /// </summary>
        [Test]
        public void Load_YamlWithNullValues_HandlesCorrectly()
        {
            // Arrange
            const string yaml = """

                key1: null
                key2: ~
                key3:
                key4: value4
                """;
            var source = new YamlConfigurationSource
            {
                Path = "nulls.yaml",
                FileProvider = new TestStreamFileProvider(yaml),
            };
            var provider = new YamlConfigurationProvider(source);

            // Act
            provider.Load();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(provider.TryGet("key1", out var value1), Is.True);
                Assert.That(value1, Is.Null);

                Assert.That(provider.TryGet("key2", out var value2), Is.True);
                Assert.That(value2, Is.Null);

                Assert.That(provider.TryGet("key3", out var value3), Is.True);
                Assert.That(value3, Is.Null);

                Assert.That(provider.TryGet("key4", out var value4), Is.True);
                Assert.That(value4, Is.EqualTo("value4"));
            });
        }

        /// <summary>
        /// Verifies that the YAML configuration provider correctly flattens YAML structures
        /// containing arrays and nested objects into a key-value representation that can be retrieved by keys.
        /// </summary>
        [Test]
        public void Load_YamlWithArrays_FlattensCorrectly()
        {
            // Arrange
            const string yaml = """

                items:
                  - name: item1
                    value: 10
                  - name: item2
                    value: 20
                simple:
                  - one
                  - two
                  - three
                """;
            var source = new YamlConfigurationSource
            {
                Path = "arrays.yaml",
                FileProvider = new TestStreamFileProvider(yaml),
            };
            var provider = new YamlConfigurationProvider(source);

            // Act
            provider.Load();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(provider.TryGet("items:0:name", out var name1), Is.True);
                Assert.That(name1, Is.EqualTo("item1"));

                Assert.That(provider.TryGet("items:0:value", out var value1), Is.True);
                Assert.That(value1, Is.EqualTo("10"));

                Assert.That(provider.TryGet("items:1:name", out var name2), Is.True);
                Assert.That(name2, Is.EqualTo("item2"));

                Assert.That(provider.TryGet("items:1:value", out var value2), Is.True);
                Assert.That(value2, Is.EqualTo("20"));

                Assert.That(provider.TryGet("simple:0", out var simple1), Is.True);
                Assert.That(simple1, Is.EqualTo("one"));

                Assert.That(provider.TryGet("simple:1", out var simple2), Is.True);
                Assert.That(simple2, Is.EqualTo("two"));

                Assert.That(provider.TryGet("simple:2", out var simple3), Is.True);
                Assert.That(simple3, Is.EqualTo("three"));
            });
        }

        /// <summary>
        /// Ensures that when the YAML configuration provider's Load method is called multiple times,
        /// existing data is replaced with the new data from the latest load call.
        /// </summary>
        [Test]
        public void Load_CalledMultipleTimes_ReplacesData()
        {
            // Arrange
            const string yaml1 = "key: value1";
            const string yaml2 = "key: value2";
            var fileProvider = new TestStreamFileProvider(yaml1);
            var source = new YamlConfigurationSource
            {
                Path = "test.yaml",
                FileProvider = fileProvider,
            };
            var provider = new YamlConfigurationProvider(source);

            // Act - Load first time
            provider.Load();
            Assert.That(provider.TryGet("key", out var value1), Is.True);
            Assert.That(value1, Is.EqualTo("value1"));

            // Update content and load again
            fileProvider.UpdateContent(yaml2);
            provider.Load();

            // Assert - Data should be replaced
            Assert.That(provider.TryGet("key", out var value2), Is.True);
            Assert.That(value2, Is.EqualTo("value2"));
        }

        /// <summary>
        /// Validates the behavior of the YAML configuration provider when loading YAML
        /// containing duplicate keys, ensuring the last encountered value overwrites the previous values.
        /// </summary>
        [Test]
        public void Load_YamlWithDuplicateKeys_LastValueWins()
        {
            // Arrange
            const string yaml = """

                key: value1
                key: value2
                """;
            var source = new YamlConfigurationSource
            {
                Path = "duplicates.yaml",
                FileProvider = new TestStreamFileProvider(yaml),
            };
            var provider = new YamlConfigurationProvider(source);

            // Act
            provider.Load();

            // Assert
            Assert.That(provider.TryGet("key", out var value), Is.True);
            Assert.That(value, Is.EqualTo("value2"));
        }

        /// <summary>
        /// Validates the behavior of the YAML configuration provider when loading YAML
        /// containing anchors and aliases, ensuring it throws a <see cref="FormatException"/>.
        /// </summary>
        [Test]
        public void Load_YamlWithAnchorsAndAliases_ThrowsFormatException()
        {
            // Arrange
            const string yaml = """

                defaults: &defaults
                  timeout: 30
                  retries: 3

                service:
                  <<: *defaults
                  name: my-service
                """;
            var source = new YamlConfigurationSource
            {
                Path = "aliases.yaml",
                FileProvider = new TestStreamFileProvider(yaml),
            };
            var provider = new YamlConfigurationProvider(source);

            // Act & Assert
            var ex = Assert.Throws<InvalidDataException>(() => provider.Load());
            Assert.That(ex.InnerException, Is.TypeOf<FormatException>());
            Assert.That(ex.InnerException!.Message, Does.Contain("aliases are not supported"));
        }

        [Test]
        public void Source_WindowsPath_PreservesPath()
        {
            // Arrange
            const string windowsPath = @"C:\Users\test\config.yaml";
            const string yaml = "test: windows path value";

            var source = new YamlConfigurationSource
            {
                Path = windowsPath,
                FileProvider = new TestStreamFileProvider(yaml),
            };

            // Act
            var provider = new YamlConfigurationProvider(source);
            provider.Load();

            // Assert
            Assert.That(provider.Source.Path, Is.EqualTo(windowsPath));
            Assert.That(provider.TryGet("test", out var value), Is.True);
            Assert.That(value, Is.EqualTo("windows path value"));
        }

        /// <summary>
        /// Test file provider that provides content from an in-memory string.
        /// </summary>
        private class TestStreamFileProvider(string content) : IFileProvider
        {
            private string content = content;

            /// <summary>
            /// Updates the content of the file provider with new content.
            /// </summary>
            /// <param name="newContent">The new content to replace the current content with.</param>
            public void UpdateContent(string newContent)
            {
                this.content = newContent;
            }

            /// <summary>
            /// Retrieves the file information for the specified subpath within the file provider.
            /// </summary>
            /// <param name="subpath">The relative path to the file for which to retrieve information.</param>
            /// <returns>An <see cref="IFileInfo"/> instance representing the file information for the specified subpath.</returns>
            public IFileInfo GetFileInfo(string subpath)
            {
                return new TestFileInfo(this.content);
            }

            /// <summary>
            /// Retrieves the directory contents for the specified subpath within the file provider.
            /// </summary>
            /// <param name="subpath">The relative path to the directory for which to retrieve contents.</param>
            /// <returns>An <see cref="IDirectoryContents"/> instance representing the contents of the specified directory.</returns>
            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Watches for changes to the file or directory specified by the filter.
            /// </summary>
            /// <param name="filter">The filter string used to identify the file or directory to watch for changes.</param>
            /// <returns>An <see cref="IChangeToken"/> instance representing a token that notifies about changes, or a token that indicates no changes if not supported.</returns>
            public IChangeToken Watch(string filter)
            {
                return NullChangeToken.Instance;
            }

            /// <summary>
            /// Represents a test file implementation that serves content from an in-memory string.
            /// Implements the <see cref="IFileInfo"/> interface to provide file metadata and content stream capabilities.
            /// </summary>
            private class TestFileInfo(string content) : IFileInfo
            {
                /// <summary>
                /// Indicates whether the file exists in the current context.
                /// </summary>
                /// <remarks>
                /// This property always returns true to simulate the existence of a file for testing purposes.
                /// </remarks>
                public bool Exists => true;

                /// <summary>
                /// Gets the length of the file in bytes.
                /// </summary>
                /// <value>
                /// A <see cref="long"/> representing the size of the content in bytes.
                /// </value>
                public long Length => Encoding.UTF8.GetByteCount(content);

                /// <summary>
                /// Gets the physical path to the file, if available.
                /// Returns null if the physical path is not defined or applicable.
                /// </summary>
                public string PhysicalPath => null!;

                /// <summary>
                /// Gets the name of the file represented by the <see cref="IFileInfo"/> instance.
                /// </summary>
                /// <value>
                /// A string that represents the name of the file.
                /// </value>
                public string Name => "test.yaml";

                /// <summary>
                /// Gets the timestamp of the last modification to the file.
                /// </summary>
                /// <value>
                /// A <see cref="DateTimeOffset"/> representing the date and time when the file was last modified.
                /// </value>
                public DateTimeOffset LastModified => DateTimeOffset.Now;

                /// <summary>
                /// Gets a value indicating whether the current file system entry represents a directory.
                /// </summary>
                /// <value>
                /// Returns <c>true</c> if the file system entry is a directory; otherwise, <c>false</c>.
                /// </value>
                public bool IsDirectory => false;

                /// <summary>
                /// Creates and returns a stream for reading the content of the file.
                /// </summary>
                /// <returns>A <see cref="Stream"/> instance that allows the content of the file to be read.</returns>
                public Stream CreateReadStream()
                {
                    return new MemoryStream(Encoding.UTF8.GetBytes(content));
                }
            }

            /// <summary>
            /// Represents a change token that never changes and does not trigger callbacks.
            /// This is used as a no-op implementation of <see cref="IChangeToken"/> to signal
            /// that there are no changes being tracked.
            /// </summary>
            private class NullChangeToken : IChangeToken
            {
                /// <summary>
                /// Represents a singleton instance of the <see cref="YamlConfigurationProviderTests.TestStreamFileProvider.NullChangeToken"/>.
                /// This instance is used to indicate a change token that does not support change notifications.
                /// </summary>
                public static readonly NullChangeToken Instance = new();

                /// <summary>
                /// Gets a value indicating whether the underlying resource has changed.
                /// </summary>
                /// <remarks>
                /// This property is typically used to determine if the content or state of the object has been modified since
                /// it was last checked. In the context of file providers, it can be used to detect changes in the watched
                /// resource. When set to <c>false</c>, it indicates that there have been no changes.
                /// </remarks>
                public bool HasChanged => false;

                /// <summary>
                /// Gets a value indicating whether change callbacks will be invoked actively.
                /// This property determines if consumers will receive notifications about
                /// changes immediately when they occur, rather than relying on polling or other mechanisms.
                /// </summary>
                public bool ActiveChangeCallbacks => false;

                /// <summary>
                /// Registers a callback to be invoked when the change token has changed.
                /// </summary>
                /// <param name="callback">The callback to invoke when a change occurs.</param>
                /// <param name="state">A user-defined object passed to the callback.</param>
                /// <returns>A disposable object that can be used to unregister the callback.</returns>
                public IDisposable RegisterChangeCallback(
                    Action<object?> callback,
                    object? state
                ) => new NullDisposable();

                /// <summary>
                /// Represents a no-operation disposable object that does nothing when disposed.
                /// </summary>
                private class NullDisposable : IDisposable
                {
                    /// <summary>
                    /// Releases the resources used by the current instance of the class.
                    /// </summary>
                    public void Dispose() { }
                }
            }
        }
    }
}
