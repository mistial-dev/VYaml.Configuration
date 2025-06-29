// <copyright file="YamlConfigurationReloadTests.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration.Test
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Primitives;
    using NUnit.Framework;

    /// <summary>
    /// Tests for the ReloadOnChange functionality of YamlConfigurationProvider.
    /// These tests verify that configuration automatically reloads when YAML files are modified.
    /// </summary>
    [TestFixture]
    public class YamlConfigurationReloadTests
    {
        /// <summary>
        /// Represents the path to a temporary directory used in the test environment.
        /// This directory is created and used during test setup to isolate test data
        /// and is deleted after the tests are completed to ensure a clean test environment.
        /// </summary>
        private string tempDirectory = null!;

        /// <summary>
        /// Represents the path to a temporary YAML configuration file used in the test environment.
        /// This file is created within a temporary directory during test setup to simulate
        /// configuration scenarios. The file is updated or modified to validate behaviors
        /// related to configuration reload and persistence.
        /// </summary>
        private string tempFilePath = null!;

        /// <summary>
        /// Waits asynchronously for a condition to be met within a specified timeout.
        /// Regularly checks the condition at a given polling interval.
        /// Fails the test if the condition is not met within the timeout period.
        /// </summary>
        /// <param name="condition">A delegate that defines the condition to be checked.</param>
        /// <param name="timeoutMs">The maximum time to wait for the condition to be met, in milliseconds. Defaults to 2000 ms.</param>
        /// <param name="pollingIntervalMs">The interval between condition checks, in milliseconds. Defaults to 20 ms.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task WaitForConditionAsync(
            Func<bool> condition,
            int timeoutMs = 2000,
            int pollingIntervalMs = 20
        )
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (condition())
                {
                    return;
                }
                await Task.Delay(pollingIntervalMs);
            }
            Assert.Fail($"Condition not met within {timeoutMs}ms");
        }

        /// <summary>
        /// Sets up the test environment by creating a temporary directory and file.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.tempDirectory = Path.Combine(
                Path.GetTempPath(),
                $"vyaml-reload-test-{Guid.NewGuid()}"
            );
            Directory.CreateDirectory(this.tempDirectory);
            this.tempFilePath = Path.Combine(this.tempDirectory, "config.yaml");
        }

        /// <summary>
        /// Cleans up the test environment by deleting the temporary directory.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(this.tempDirectory))
            {
                try
                {
                    Directory.Delete(this.tempDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Verifies that configuration automatically reloads when the YAML file is modified
        /// and ReloadOnChange is set to true.
        /// </summary>
        [Test]
        public async Task Configuration_ReloadsAutomatically_WhenFileIsModified()
        {
            // Arrange
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: value1\nsetting2: 123");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.GetFileName(this.tempFilePath),
                    optional: false,
                    reloadOnChange: true
                )
                .Build();

            // Act & Assert - Initial values
            Assert.That(configuration["setting1"], Is.EqualTo("value1"));
            Assert.That(configuration["setting2"], Is.EqualTo("123"));

            // Modify the file and wait for reload
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: newvalue\nsetting2: 456");
            await WaitForConditionAsync(() =>
                configuration["setting1"] == "newvalue" && configuration["setting2"] == "456"
            );

            // Assert - Values should be updated
            Assert.That(configuration["setting1"], Is.EqualTo("newvalue"));
            Assert.That(configuration["setting2"], Is.EqualTo("456"));
        }

        /// <summary>
        /// Verifies that configuration does NOT reload when ReloadOnChange is false.
        /// </summary>
        [Test]
        public async Task Configuration_DoesNotReload_WhenReloadOnChangeIsFalse()
        {
            // Arrange
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: value1");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.GetFileName(this.tempFilePath),
                    optional: false,
                    reloadOnChange: false
                )
                .Build();

            // Act & Assert - Initial value
            Assert.That(configuration["setting1"], Is.EqualTo("value1"));

            // Modify the file
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: newvalue");

            // Short pause to ensure no reload happens
            await Task.Delay(200);

            // Assert - Value should NOT be updated
            Assert.That(configuration["setting1"], Is.EqualTo("value1"));
        }

        /// <summary>
        /// Verifies that the configuration change token fires when the file is modified.
        /// </summary>
        [Test]
        public async Task ChangeToken_FiresCallback_WhenFileIsModified()
        {
            // Arrange
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: value1");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.GetFileName(this.tempFilePath),
                    optional: false,
                    reloadOnChange: true
                )
                .Build();

            var changeDetected = false;
            var resetEvent = new ManualResetEventSlim(false);

            // Register change callback
            ChangeToken.OnChange(
                () => configuration.GetReloadToken(),
                () =>
                {
                    changeDetected = true;
                    resetEvent.Set();
                }
            );

            // Act - Modify the file
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: newvalue");

            // Assert - Wait for change notification
            var wasSignaled = resetEvent.Wait(TimeSpan.FromSeconds(5));
            Assert.That(wasSignaled, Is.True, "Change token callback was not fired within timeout");
            Assert.That(changeDetected, Is.True);
            Assert.That(configuration["setting1"], Is.EqualTo("newvalue"));
        }

        /// <summary>
        /// Verifies that nested configuration values are properly updated on reload.
        /// </summary>
        [Test]
        public async Task NestedConfiguration_UpdatesProperly_OnReload()
        {
            // Arrange
            const string initialYaml = """

                database:
                  connectionString: Server=localhost;Database=test
                  timeout: 30
                features:
                  enabled: true
                  items:
                    - item1
                    - item2

                """;
            await File.WriteAllTextAsync(this.tempFilePath, initialYaml);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.GetFileName(this.tempFilePath),
                    optional: false,
                    reloadOnChange: true
                )
                .Build();

            // Verify initial values
            Assert.That(
                configuration["database:connectionString"],
                Is.EqualTo("Server=localhost;Database=test")
            );
            Assert.That(configuration["database:timeout"], Is.EqualTo("30"));
            Assert.That(configuration["features:enabled"], Is.EqualTo("true"));
            Assert.That(configuration["features:items:0"], Is.EqualTo("item1"));

            // Act - Update with new nested values
            var updatedYaml = """

                database:
                  connectionString: Server=prod;Database=production
                  timeout: 60
                  poolSize: 100
                features:
                  enabled: false
                  items:
                    - newitem1
                    - newitem2
                    - newitem3

                """;
            await File.WriteAllTextAsync(this.tempFilePath, updatedYaml);
            await WaitForConditionAsync(() =>
                configuration["database:connectionString"] == "Server=prod;Database=production"
            );

            // Assert - All values should be updated
            Assert.Multiple(() =>
            {
                Assert.That(
                    configuration["database:connectionString"],
                    Is.EqualTo("Server=prod;Database=production")
                );
                Assert.That(configuration["database:timeout"], Is.EqualTo("60"));
                Assert.That(configuration["database:poolSize"], Is.EqualTo("100"));
                Assert.That(configuration["features:enabled"], Is.EqualTo("false"));
                Assert.That(configuration["features:items:0"], Is.EqualTo("newitem1"));
                Assert.That(configuration["features:items:2"], Is.EqualTo("newitem3"));
            });
        }

        /// <summary>
        /// Verifies that configuration handles invalid YAML gracefully during reload.
        /// When invalid YAML is encountered, the configuration is cleared rather than
        /// retaining old values, which is the behavior of FileConfigurationProvider.
        /// </summary>
        [Test]
        public async Task Configuration_HandlesInvalidYaml_DuringReload()
        {
            // Arrange
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: value1");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.GetFileName(this.tempFilePath),
                    optional: false,
                    reloadOnChange: true
                )
                .Build();

            Assert.That(configuration["setting1"], Is.EqualTo("value1"));

            // Act - Write invalid YAML
            await File.WriteAllTextAsync(this.tempFilePath, "invalid: yaml: content: :");
            await WaitForConditionAsync(() => configuration["setting1"] == null);

            // Assert - Configuration is cleared on error (FileConfigurationProvider behavior)
            Assert.That(configuration["setting1"], Is.Null);

            // Act - Write valid YAML again
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: recovered");
            await WaitForConditionAsync(() => configuration["setting1"] == "recovered");

            // Assert - Configuration should recover
            Assert.That(configuration["setting1"], Is.EqualTo("recovered"));
        }

        /// <summary>
        /// Verifies that multiple rapid file changes are handled correctly.
        /// </summary>
        [Test]
        public async Task Configuration_HandlesRapidFileChanges_Correctly()
        {
            // Arrange
            await File.WriteAllTextAsync(this.tempFilePath, "counter: 0");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.GetFileName(this.tempFilePath),
                    optional: false,
                    reloadOnChange: true
                )
                .Build();

            // Act - Make rapid changes
            for (int i = 1; i <= 5; i++)
            {
                await File.WriteAllTextAsync(this.tempFilePath, $"counter: {i}");
                await Task.Delay(50); // Small delay between changes
            }

            // Wait for final change to be processed
            await WaitForConditionAsync(() => configuration["counter"] == "5");

            // Assert - Should have the last value
            Assert.That(configuration["counter"], Is.EqualTo("5"));
        }

        /// <summary>
        /// Verifies that file deletion is handled gracefully when optional is true.
        /// </summary>
        [Test]
        public async Task Configuration_HandlesFileDeletion_WhenOptionalIsTrue()
        {
            // Arrange
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: value1");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.GetFileName(this.tempFilePath),
                    optional: true,
                    reloadOnChange: true
                )
                .Build();

            Assert.That(configuration["setting1"], Is.EqualTo("value1"));

            // Act - Delete the file
            File.Delete(this.tempFilePath);
            await WaitForConditionAsync(() => configuration["setting1"] == null);

            // Assert - Configuration should be empty but not throw
            Assert.That(configuration["setting1"], Is.Null);
        }

        /// <summary>
        /// Verifies that file recreation after deletion reloads configuration.
        /// </summary>
        [Test]
        public async Task Configuration_ReloadsWhenFileIsRecreated_AfterDeletion()
        {
            // Arrange
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: original");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.GetFileName(this.tempFilePath),
                    optional: true,
                    reloadOnChange: true
                )
                .Build();

            // Delete and recreate
            File.Delete(this.tempFilePath);
            await WaitForConditionAsync(() => configuration["setting1"] == null);

            await File.WriteAllTextAsync(this.tempFilePath, "setting1: recreated");
            await WaitForConditionAsync(() => configuration["setting1"] == "recreated");

            // Assert
            Assert.That(configuration["setting1"], Is.EqualTo("recreated"));
        }

        /// <summary>
        /// Verifies that configuration sections are updated correctly on reload.
        /// </summary>
        [Test]
        public async Task ConfigurationSection_UpdatesCorrectly_OnReload()
        {
            // Arrange
            var yaml = """

                app:
                  name: TestApp
                  version: 1.0.0
                logging:
                  level: Information

                """;
            await File.WriteAllTextAsync(this.tempFilePath, yaml);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.GetFileName(this.tempFilePath),
                    optional: false,
                    reloadOnChange: true
                )
                .Build();

            var appSection = configuration.GetSection("app");
            Assert.That(appSection["name"], Is.EqualTo("TestApp"));
            Assert.That(appSection["version"], Is.EqualTo("1.0.0"));

            // Act - Update configuration
            var updatedYaml = """

                app:
                  name: UpdatedApp
                  version: 2.0.0
                  description: New feature
                logging:
                  level: Debug

                """;
            await File.WriteAllTextAsync(this.tempFilePath, updatedYaml);
            await Task.Delay(500);

            // Assert - Section values should be updated
            Assert.That(appSection["name"], Is.EqualTo("UpdatedApp"));
            Assert.That(appSection["version"], Is.EqualTo("2.0.0"));
            Assert.That(appSection["description"], Is.EqualTo("New feature"));
        }

        /// <summary>
        /// Verifies that concurrent reads during file modification work correctly.
        /// </summary>
        [Test]
        public async Task ConcurrentReads_DuringFileModification_WorkCorrectly()
        {
            // Arrange
            await File.WriteAllTextAsync(this.tempFilePath, "counter: 0");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.GetFileName(this.tempFilePath),
                    optional: false,
                    reloadOnChange: true
                )
                .Build();

            var successfulReads = 0;
            var cts = new CancellationTokenSource();

            // Start concurrent readers
            var readTasks = new Task[5];
            for (int i = 0; i < readTasks.Length; i++)
            {
                readTasks[i] = Task.Run(
                    async () =>
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                var value = configuration["counter"];
                                if (value != null)
                                {
                                    Interlocked.Increment(ref successfulReads);
                                }

                                await Task.Delay(10, cts.Token);
                            }
                            catch (OperationCanceledException)
                            {
                                // Expected when cancellation is requested
                                break;
                            }
                        }
                    },
                    cts.Token
                );
            }

            // Modify file multiple times
            for (int i = 1; i <= 10; i++)
            {
                await File.WriteAllTextAsync(this.tempFilePath, $"counter: {i}", cts.Token);
                await Task.Delay(100, cts.Token);
            }

            // Stop readers
            await cts.CancelAsync();
            try
            {
                await Task.WhenAll(readTasks);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert - Should have many successful reads and final value should be correct
            Assert.That(
                successfulReads,
                Is.GreaterThan(0),
                "Should have successful reads during modifications"
            );
            Assert.That(configuration["counter"], Is.EqualTo("10"));
        }

        /// <summary>
        /// Verifies that empty YAML files are handled correctly during reload.
        /// </summary>
        [Test]
        public async Task EmptyFile_HandledCorrectly_DuringReload()
        {
            // Arrange
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: value1");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddYamlFile(
                    Path.GetFileName(this.tempFilePath),
                    optional: false,
                    reloadOnChange: true
                )
                .Build();

            Assert.That(configuration["setting1"], Is.EqualTo("value1"));

            // Act - Write empty file
            await File.WriteAllTextAsync(this.tempFilePath, string.Empty);
            await Task.Delay(500);

            // Assert - Configuration should be empty
            Assert.That(configuration["setting1"], Is.Null);

            // Act - Write new content
            await File.WriteAllTextAsync(this.tempFilePath, "setting1: newvalue");
            await Task.Delay(500);

            // Assert - Configuration should be updated
            Assert.That(configuration["setting1"], Is.EqualTo("newvalue"));
        }
    }
}
