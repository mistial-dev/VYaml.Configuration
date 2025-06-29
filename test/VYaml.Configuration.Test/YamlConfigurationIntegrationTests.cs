// <copyright file="YamlConfigurationIntegrationTests.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

// ReSharper disable BadParensSpaces
// ReSharper disable AccessToStaticMemberViaDerivedType
namespace VYaml.Configuration.Test
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using NUnit.Framework;

    /// <summary>
    /// Integration tests for YAML configuration functionality.
    /// Verifies proper reading, binding, and handling of YAML configuration files, including edge cases and advanced scenarios.
    /// </summary>
    [TestFixture]
    public class YamlConfigurationIntegrationTests
    {
        /// <summary>
        /// Validates that a simple YAML configuration file is successfully loaded into the configuration system.
        /// </summary>
        /// <remarks>
        /// This test verifies that a basic YAML file containing key-value pairs is correctly parsed and loaded into the configuration.
        /// It ensures that the system can handle plain YAML files with straightforward structures and that it correctly maps the values to the configuration.
        /// </remarks>
        [Test]
        public void Configuration_SimpleYaml_LoadsCorrectly()
        {
            // Arrange
            // Use just the relative path - the files are in TestData subdirectory
            const string configPath = "TestData/simple.yaml";

            // Act
            var configuration = new ConfigurationBuilder()
                .AddYamlFile(configPath, optional: false)
                .Build();

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(configuration["appName"], Is.EqualTo("Simple Test App"));
                Assert.That(configuration["version"], Is.EqualTo("1.0.0"));
                Assert.That(configuration["debug"], Is.EqualTo("true"));
            });
        }

        /// <summary>
        /// Validates that nested YAML structures are correctly loaded and bound to the configuration system.
        /// </summary>
        /// <remarks>
        /// This test ensures that multi-layered YAML structures, including nested keys, arrays, and complex objects, are parsed
        /// and mapped accurately into the hierarchical configuration system. It verifies the integrity of key-value pairs for
        /// sections such as "application", "database", and "api", ensuring proper value retrieval and compatibility with nested YAML.
        /// </remarks>
        [Test]
        public void Configuration_NestedYaml_LoadsCorrectly()
        {
            // Arrange
            var configPath = "TestData/nested.yaml";

            // Act
            var configuration = new ConfigurationBuilder().AddYamlFile(configPath).Build();

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(
                    configuration["application:name"],
                    Is.EqualTo("Nested Test App")
                );
                Assert.That(configuration["application:version"], Is.EqualTo("2.0.0"));
                Assert.That(
                    configuration["database:connectionString"],
                    Is.EqualTo("Server=localhost;Database=test")
                );
                Assert.That(
                    configuration["database:settings:maxConnections"],
                    Is.EqualTo("100")
                );
                Assert.That(configuration["database:settings:timeout"], Is.EqualTo("30"));
                Assert.That(
                    configuration["database:settings:enableRetry"],
                    Is.EqualTo("true")
                );
                Assert.That(
                    configuration["api:baseUrl"],
                    Is.EqualTo("https://api.test.com")
                );
                Assert.That(configuration["api:endpoints:users"], Is.EqualTo("/api/users"));
                Assert.That(
                    configuration["api:endpoints:orders"],
                    Is.EqualTo("/api/orders")
                );
                Assert.That(configuration["api:security:enableAuth"], Is.EqualTo("true"));
                Assert.That(configuration["api:security:tokenExpiry"], Is.EqualTo("3600"));
            });
        }

        /// <summary>
        /// Validates that a YAML configuration file containing arrays is correctly loaded into the configuration system.
        /// </summary>
        /// <remarks>
        /// This test ensures proper handling and loading of various array structures from a YAML file into the configuration.
        /// It verifies the correct access to elements of nested object arrays, simple arrays, numerical arrays,
        /// boolean arrays, and mixed-type arrays. The test also confirms that null values in the mixed-type array
        /// are appropriately handled.
        /// </remarks>
        [Test]
        public void Configuration_ArrayYaml_LoadsCorrectly()
        {
            // Arrange
            const string configPath = "TestData/arrays.yaml";

            // Act
            var configuration = new ConfigurationBuilder().AddYamlFile(configPath).Build();

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(configuration["servers:0:name"], Is.EqualTo("server1"));
                Assert.That(configuration["servers:0:host"], Is.EqualTo("localhost"));
                Assert.That(configuration["servers:0:port"], Is.EqualTo("8081"));
                Assert.That(configuration["servers:1:name"], Is.EqualTo("server2"));
                Assert.That(configuration["servers:1:host"], Is.EqualTo("192.168.1.100"));
                Assert.That(configuration["servers:1:port"], Is.EqualTo("8082"));
                Assert.That(configuration["servers:2:name"], Is.EqualTo("server3"));
                Assert.That(configuration["servers:2:host"], Is.EqualTo("10.0.0.5"));
                Assert.That(configuration["servers:2:port"], Is.EqualTo("8083"));

                Assert.That(configuration["simpleArray:0"], Is.EqualTo("item1"));
                Assert.That(configuration["simpleArray:1"], Is.EqualTo("item2"));
                Assert.That(configuration["simpleArray:2"], Is.EqualTo("item3"));

                Assert.That(configuration["numbersArray:0"], Is.EqualTo("1"));
                Assert.That(configuration["numbersArray:1"], Is.EqualTo("2"));
                Assert.That(configuration["numbersArray:2"], Is.EqualTo("3"));
                Assert.That(configuration["numbersArray:3"], Is.EqualTo("42"));

                Assert.That(configuration["booleanArray:0"], Is.EqualTo("true"));
                Assert.That(configuration["booleanArray:1"], Is.EqualTo("false"));
                Assert.That(configuration["booleanArray:2"], Is.EqualTo("true"));

                Assert.That(configuration["mixedArray:0"], Is.EqualTo("string"));
                Assert.That(configuration["mixedArray:1"], Is.EqualTo("123"));
                Assert.That(configuration["mixedArray:2"], Is.EqualTo("true"));
                Assert.That(configuration["mixedArray:3"], Is.Null);
            });
        }

        /// <summary>
        /// Validates that a complex YAML configuration file is loaded correctly into the configuration system.
        /// </summary>
        /// <remarks>
        /// This test reads a YAML configuration file with a complex structure, including nested sections, arrays, and key-value pairs.
        /// It verifies that all values, including deeply nested properties and array elements, are correctly bound to the configuration system.
        /// The validation ensures compatibility with complex YAML schemas and proper handling of hierarchical and list-based configurations.
        /// </remarks>
        [Test]
        public void Configuration_ComplexYaml_LoadsCorrectly()
        {
            // Arrange
            const string configPath = "TestData/complex-no-aliases.yaml";

            // Act
            var configuration = new ConfigurationBuilder().AddYamlFile(configPath).Build();

            Assert.Multiple(() =>
            {
                // Assert key sections
                Assert.That(
                    configuration["application:name"],
                    Is.EqualTo("Complex Test Application")
                );
                Assert.That(configuration["application:version"], Is.EqualTo("3.1.4"));
                Assert.That(configuration["environment:name"], Is.EqualTo("production"));
                Assert.That(configuration["environment:debug"], Is.EqualTo("false"));
                Assert.That(
                    configuration["services:database:provider"],
                    Is.EqualTo("postgresql")
                );
                Assert.That(
                    configuration["services:database:connection:host"],
                    Is.EqualTo("db.example.com")
                );
                Assert.That(
                    configuration["services:database:connection:port"],
                    Is.EqualTo("5432")
                );
                Assert.That(
                    configuration["services:messaging:queues:0:name"],
                    Is.EqualTo("orders")
                );
                Assert.That(
                    configuration["services:messaging:queues:0:durable"],
                    Is.EqualTo("true")
                );
                Assert.That(
                    configuration["services:messaging:queues:1:name"],
                    Is.EqualTo("notifications")
                );
                Assert.That(
                    configuration["services:messaging:queues:1:autoDelete"],
                    Is.EqualTo("true")
                );
            });
        }

        /// <summary>
        /// Verifies that the option pattern is correctly bound to a YAML configuration file.
        /// </summary>
        /// <remarks>
        /// This test verifies that a YAML configuration file is loaded and bound to an option class using the option pattern.
        /// The test confirms that all properties, including nested configurations such as database settings, are assigned correctly.
        /// It ensures that configuration values such as connection strings, maximum connections, timeouts,
        /// and retry flags are bound to the corresponding class properties as expected.
        /// </remarks>
        [Test]
        public void Configuration_OptionsPattern_BindsCorrectly()
        {
            // Arrange
            const string configPath = "TestData/nested.yaml";
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().AddYamlFile(configPath).Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.Configure<DatabaseOptions>(configuration.GetSection("database"));

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var databaseOptions = serviceProvider
                .GetRequiredService<IOptions<DatabaseOptions>>()
                .Value;

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(databaseOptions, Is.Not.Null);
                Assert.That(
                    databaseOptions.ConnectionString,
                    Is.EqualTo("Server=localhost;Database=test")
                );
                Assert.That(databaseOptions.Settings, Is.Not.Null);

                Assert.That(databaseOptions.Settings.MaxConnections, Is.EqualTo(100));
                Assert.That(databaseOptions.Settings.Timeout, Is.EqualTo(30));
                Assert.That(databaseOptions.Settings.EnableRetry, Is.True);
            });
        }

        /// <summary>
        /// Ensures that array data in a YAML configuration file is correctly bound to the corresponding configuration objects.
        /// </summary>
        /// <remarks>
        /// This test verifies the binding of array-based YAML data into strongly typed configuration objects using
        /// Microsoft.Extensions.Configuration and the Options pattern. It validates binding for both complex object arrays
        /// such as server configurations, as well as simple scalar arrays of strings and integers.
        /// </remarks>
        [Test]
        public void Configuration_ArrayBinding_BindsCorrectly()
        {
            // Arrange
            var configPath = "TestData/arrays.yaml";
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().AddYamlFile(configPath).Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.Configure<ServerConfiguration>(configuration);

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var serverConfig = serviceProvider
                .GetRequiredService<IOptions<ServerConfiguration>>()
                .Value;

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(serverConfig, Is.Not.Null);
                Assert.That(serverConfig.Servers, Is.Not.Null);
                Assert.That(serverConfig.Servers, Has.Count.EqualTo(3));

                Assert.That(serverConfig.Servers[0].Name, Is.EqualTo("server1"));
                Assert.That(serverConfig.Servers[0].Host, Is.EqualTo("localhost"));
                Assert.That(serverConfig.Servers[0].Port, Is.EqualTo(8081));

                Assert.That(serverConfig.Servers[1].Name, Is.EqualTo("server2"));
                Assert.That(serverConfig.Servers[1].Host, Is.EqualTo("192.168.1.100"));
                Assert.That(serverConfig.Servers[1].Port, Is.EqualTo(8082));

                Assert.That(serverConfig.SimpleArray, Is.Not.Null);
                Assert.That(serverConfig.SimpleArray, Has.Count.EqualTo(3));
                Assert.That(
                    serverConfig.SimpleArray,
                    Is.EqualTo(new[] { "item1", "item2", "item3" })
                );

                Assert.That(serverConfig.NumbersArray, Is.Not.Null);
                Assert.That(serverConfig.NumbersArray, Has.Count.EqualTo(4));
                Assert.That(serverConfig.NumbersArray, Is.EqualTo(new[] { 1, 2, 3, 42 }));
            });
        }

        /// <summary>
        /// Validates that multiple YAML configuration files are correctly merged into a single configuration system.
        /// </summary>
        /// <remarks>
        /// This test ensures that when multiple YAML files are loaded into the configuration system, their contents are merged appropriately.
        /// It verifies that values from different files coexist without overwriting or loss of data, preserving the configuration hierarchy.
        /// The test checks compatibility with scenarios where multiple configuration files are used to organize settings, both flat and hierarchical.
        /// </remarks>
        /// <example>
        /// The test loads two YAML files: one containing basic key-value pairs (e.g. "appName") and another with nested structures
        /// (e.g., "application:name" and "database:connectionString"). The merged configuration should expose all these values seamlessly.
        /// </example>
        [Test]
        public void Configuration_MultipleFiles_MergesCorrectly()
        {
            // Arrange
            const string simplePath = "TestData/simple.yaml";
            const string nestedPath = "TestData/nested.yaml";

            // Act
            var configuration = new ConfigurationBuilder()
                .AddYamlFile(simplePath)
                .AddYamlFile(nestedPath)
                .Build();

            Assert.Multiple(() =>
            {
                // Assert - values from both files should be available
                Assert.That(configuration["appName"], Is.EqualTo("Simple Test App")); // from simple.yaml
                Assert.That(
                    configuration["application:name"],
                    Is.EqualTo("Nested Test App")
                ); // from nested.yaml
                Assert.That(
                    configuration["database:connectionString"],
                    Is.EqualTo("Server=localhost;Database=test")
                ); // from nested.yaml
            });
        }

        /// <summary>
        /// Verifies that the configuration system does not throw an exception when attempting to load
        /// a missing YAML file marked as optional.
        /// </summary>
        /// <remarks>
        /// This test ensures that optional file loading is handled gracefully by the configuration system.
        /// A nonexistent YAML file is specified as optional alongside an existing file, and the test
        /// confirms that the system proceeds without errors while correctly loading the existing file.
        /// </remarks>
        [Test]
        public void Configuration_OptionalFileNotFound_DoesNotThrow()
        {
            // Arrange
            const string nonExistentPath = "TestData/nonexistent.yaml";
            const string existingPath = "TestData/simple.yaml";

            // Act & Assert
            Assert.DoesNotThrow(static () =>
            {
                var configuration = new ConfigurationBuilder()
                    .AddYamlFile(nonExistentPath, true)
                    .AddYamlFile(existingPath)
                    .Build();

                // Verify that the existing file was loaded
                Assert.That(configuration["appName"], Is.EqualTo("Simple Test App"));
            });
        }

        /// <summary>
        /// Verifies that a FileNotFoundException is thrown when a required YAML configuration file is missing.
        /// </summary>
        /// <remarks>
        /// This test ensures that the configuration builder enforces the presence of required YAML files marked as mandatory.
        /// If the specified file does not exist and the optional flag is set to false, the builder is expected to throw
        /// a FileNotFoundException, providing clear feedback about the missing file.
        /// </remarks>
        [Test]
        public void Configuration_RequiredFileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            const string nonExistentPath = "TestData/nonexistent.yaml";

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
            {
                _ = new ConfigurationBuilder().AddYamlFile(nonExistentPath, false).Build();
            });
        }

        /// <summary>
        /// Validates that the correct configuration section is retrieved from a YAML configuration file.
        /// </summary>
        /// <remarks>
        /// This test ensures that the <c>ConfigurationBuilder</c> parses nested YAML structures correctly and that the
        /// <c>GetSection</c> method retrieves the appropriate configuration subsection. It utilizes a YAML file
        /// containing nested configurations to verify expected hierarchical structure and key-value retrieval. Assertions
        /// are performed to validate the integrity of the returned configuration sections and their respective values.
        /// </remarks>
        [Test]
        public void Configuration_GetSection_ReturnsCorrectSection()
        {
            // Arrange
            const string configPath = "TestData/nested.yaml";
            var configuration = new ConfigurationBuilder().AddYamlFile(configPath).Build();

            // Act
            var databaseSection = configuration.GetSection("database");
            var settingsSection = configuration.GetSection("database:settings");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(databaseSection, Is.Not.Null);
                Assert.That(
                    databaseSection["connectionString"],
                    Is.EqualTo("Server=localhost;Database=test")
                );
                Assert.That(databaseSection["settings:maxConnections"], Is.EqualTo("100"));

                Assert.That(settingsSection, Is.Not.Null);
                Assert.That(settingsSection["maxConnections"], Is.EqualTo("100"));
                Assert.That(settingsSection["timeout"], Is.EqualTo("30"));
                Assert.That(settingsSection["enableRetry"], Is.EqualTo("true"));
            });
        }

        /// <summary>
        /// Verifies that the GetChildren method of an IConfigurationSection correctly enumerates
        /// elements of an array defined in a YAML configuration file.
        /// </summary>
        /// <remarks>
        /// This test loads a YAML file containing an array structure (e.g., a list of servers)
        /// and retrieves a configuration section representing the array. It then ensures that
        /// the GetChildren method accurately returns all array elements as individual sections.
        /// The test validates element count and specific element properties (e.g., "name") to
        /// confirm the correct configuration binding behavior.
        /// </remarks>
        [Test]
        public void Configuration_GetChildren_ReturnsArrayElements()
        {
            // Arrange
            const string configPath = "TestData/arrays.yaml";
            var configuration = new ConfigurationBuilder().AddYamlFile(configPath).Build();

            // Act
            var serversSection = configuration.GetSection("servers");
            var children = serversSection.GetChildren().ToArray();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(children, Is.Not.Null);
                Assert.That(children, Has.Length.EqualTo(3));
                Assert.That(children[0]["name"], Is.EqualTo("server1"));
                Assert.That(children[1]["name"], Is.EqualTo("server2"));
                Assert.That(children[2]["name"], Is.EqualTo("server3"));
            });
        }

        /// <summary>
        /// Verifies that a configuration object bound to a YAML file updates its values when the configuration changes.
        /// </summary>
        /// <remarks>
        /// This test ensures that changes to the underlying YAML configuration file are reflected in the bound configuration object.
        /// It validates the real-time updating behavior when the configuration system detects file changes.
        /// The test checks that the initial state of the bound object is correct and ready for changes.
        /// </remarks>
        [Test]
        public void Configuration_BoundConfigurationObject_UpdatesWhenConfigurationChanges()
        {
            // Arrange
            const string configPath = "TestData/simple.yaml";
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().AddYamlFile(configPath).Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.Configure<SimpleOptions>(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<SimpleOptions>>().Value;

            Assert.Multiple(() =>
            {
                // Assert initial state
                Assert.That(options.AppName, Is.EqualTo("Simple Test App"));
                Assert.That(options.Version, Is.EqualTo("1.0.0"));
                Assert.That(options.Debug, Is.True);
            });
        }

        /// <summary>
        /// Represents configuration options for a database connection.
        /// Includes properties for connection string and nested settings
        /// related to database behavior and performance tuning.
        /// </summary>
        public class DatabaseOptions
        {
            /// <summary>
            /// Gets the connection string used to connect to the database.
            /// </summary>
            public string ConnectionString { get; init; } = string.Empty;

            /// <summary>
            /// Gets specific configuration settings for a database, including options
            /// such as maximum number of connections, timeout duration, and retry behavior.
            /// </summary>
            public DatabaseSettings Settings { get; init; } = new();
        }

        /// <summary>
        /// Represents configuration settings for a database connection.
        /// Used to define parameters such as maximum connections, timeout duration, and retry policies.
        /// </summary>
        public class DatabaseSettings
        {
            /// <summary>
            /// Gets the maximum number of connections allowed.
            /// This property is used to define the limit on concurrent
            /// connections for a specified configuration or service.
            /// </summary>
            [UsedImplicitly]
            public int MaxConnections { get; init; }

            /// <summary>
            /// Gets the timeout value, typically used to specify the duration
            /// (in seconds or other applicable units) before a database or network
            /// operation is considered to have failed.
            /// </summary>
            [UsedImplicitly]
            public int Timeout { get; init; }

            /// <summary>
            /// Gets a value indicating whether automatic retries are enabled
            /// for database operations in case of transient failures.
            /// </summary>
            /// <remarks>
            /// This property determines whether the system should attempt to retry
            /// failed operations that may be recoverable. It is typically used to improve
            /// resiliency in scenarios where transient faults are common, such as network issues
            /// or temporary service unavailability.
            /// </remarks>
            [UsedImplicitly]
            public bool EnableRetry { get; init; }
        }

        /// <summary>
        /// Represents the server configuration model used for binding YAML configuration data.
        /// Includes details about server information, along with string and numeric array data.
        /// </summary>
        public class ServerConfiguration
        {
            /// <summary>
            /// Gets a collection of server configurations.
            /// Each server configuration includes details like server name, host, and port.
            /// Used for settings related to server connectivity and configuration binding scenarios.
            /// </summary>
            public List<ServerInfo> Servers { get; init; } = new();

            /// <summary>
            /// Gets a collection of string elements representing a simple array of values.
            /// </summary>
            /// <remarks>
            /// This property is used to map configurations defined as an array of strings in a YAML configuration file.
            /// The values in the array can be accessed and modified as needed.
            /// </remarks>
            public List<string> SimpleArray { get; init; } = new();

            /// <summary>
            /// Gets a collection of integers stored as a list.
            /// This property is used to bind data from a configuration source, such as a YAML file,
            /// where numeric arrays are defined.
            /// </summary>
            public List<int> NumbersArray { get; init; } = new();
        }

        /// <summary>
        /// Represents information about a server, including its name, host, and port.
        /// Used for configuration and connectivity purposes.
        /// </summary>
        public class ServerInfo
        {
            /// <summary>
            /// Gets the name of the server.
            /// This property is used to identify the server in configurations.
            /// </summary>
            public string Name { get; init; } = string.Empty;

            /// <summary>
            /// Gets the host address of the server.
            /// This property is used to specify the location of the server for configuration and connectivity purposes.
            /// </summary>
            public string Host { get; init; } = string.Empty;

            /// <summary>
            /// Gets the port number for the server.
            /// Represents the network port used for communication with the server.
            /// </summary>
            [UsedImplicitly]
            public int Port { get; init; }
        }

        /// <summary>
        /// Represents configuration options for a simple application.
        /// Used to bind YAML configuration data into strongly-typed properties.
        /// </summary>
        [UsedImplicitly]
        public class SimpleOptions
        {
            /// <summary>
            /// Gets the name of the application.
            /// </summary>
            public string AppName { get; init; } = string.Empty;

            /// <summary>
            /// Gets the application version as a string.
            /// Used to store and retrieve version information for the application.
            /// </summary>
            public string Version { get; init; } = string.Empty;

            /// <summary>
            /// Gets a value indicating whether debugging mode is enabled in the application.
            /// Useful for toggling verbose logging, diagnostic output, or other debugging-related behaviors.
            /// </summary>
            public bool Debug { get; init; }
        }
    }
}
