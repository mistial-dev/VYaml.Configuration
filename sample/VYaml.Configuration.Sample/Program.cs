// <copyright file="Program.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration.Sample;

using System;
using Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Services;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Main program entry point.
/// This sample application demonstrates the usage of VYaml.Configuration
/// with Microsoft.Extensions.Configuration for YAML-based configuration.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main application entry point.
    /// Sets up dependency injection, configuration, and command-line interface.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code. 0 for success, non-zero for errors.</returns>
    public static int Main(string[] args)
    {
        // Step 1: Create a service collection for dependency injection
        // This allows us to register services and resolve them throughout the application
        var services = new ServiceCollection();

        // Step 2: Configure logging
        // Set up console logging with appropriate log levels
        services.AddLogging(static builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Step 3: Configure the configuration system with YAML support
        // This demonstrates the key feature of VYaml.Configuration
        var configuration = new ConfigurationBuilder()
            // Load base configuration file (optional allows the app to run without it)
            .AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: true)
            // Load environment-specific configuration (overrides base config)
            .AddYamlFile("appsettings.Development.yaml", optional: true, reloadOnChange: true)
            // Add environment variables as the highest priority configuration source
            .AddEnvironmentVariables()
            .Build();

        // Register the configuration instance for dependency injection
        services.AddSingleton<IConfiguration>(configuration);

        // Step 4: Register application services
        // Configure strongly-typed options using the Options pattern
        services.Configure<SampleOptions>(configuration.GetSection("Sample"));

        // Register IOptionsMonitor for configuration change monitoring
        services.AddSingleton(typeof(IOptionsMonitor<>), typeof(OptionsMonitor<>));

        // Register console for pretty output
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

        // Register our custom configuration service
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Step 5: Set up the command-line interface using Spectre.Console.Cli
        // Create a type registrar that bridges Spectre.Console.Cli with Microsoft.Extensions.DependencyInjection
        var registrar = new TypeRegistrar(services);

        // Create and configure the command app
        var app = new CommandApp(registrar);
        app.Configure(static config =>
        {
            // Register the 'show' command to display current configuration
            config
                .AddCommand<ShowConfigCommand>("show")
                .WithDescription("Display current configuration values")
                .WithExample("show");

            // Register the 'validate' command to validate YAML files
            config
                .AddCommand<ValidateConfigCommand>("validate")
                .WithDescription("Validate YAML configuration file")
                .WithExample("validate", "--file", "appsettings.yaml");

            // Register the 'watch' command to demonstrate real-time configuration updates
            config
                .AddCommand<WatchConfigCommand>("watch")
                .WithDescription("Watch real-time configuration changes demo")
                .WithExample("watch")
                .WithExample("watch", "--interval", "2000");
            // Register the 'example' command to inspect any YAML file
            config
                .AddCommand<ExampleConfigCommand>("example")
                .WithDescription(
                    "Load a YAML example file and display its flattened key/value pairs"
                )
                .WithExample("example", "examples/advanced-types.yaml");

            // Set application metadata
            config.SetApplicationName("vyaml-sample");
            config.SetApplicationVersion("1.0.0");

#if DEBUG
            // In debug builds, show full exception details and validate command examples
            config.PropagateExceptions();
            config.ValidateExamples();
#endif
        });

        // Step 6: Run the application and handle any errors
        try
        {
            // Execute the command based on provided arguments
            return app.Run(args);
        }
        catch (Exception ex)
        {
            // Display exceptions in a user-friendly format
            AnsiConsole.WriteException(ex);
            return -1;
        }
    }
}
