// <copyright file="ShowConfigCommand.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration.Sample.Commands;

using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Services;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Command to show current configuration values.
/// This command demonstrates:
/// - Loading configuration from YAML files
/// - Binding configuration to strongly-typed options
/// - Displaying configuration in a user-friendly format
/// - Masking sensitive configuration values
/// </summary>
/// <example>
/// Usage: dotnet run -- show
/// </example>
[UsedImplicitly]
public class ShowConfigCommand : Command
{
    /// <summary>
    /// Represents the console interface used for writing output and interacting
    /// with the terminal in the context of the ShowConfigCommand class.
    /// </summary>
    /// <remarks>
    /// This variable provides an implementation of the <see cref="IAnsiConsole"/>
    /// interface, allowing rich terminal output capabilities such as text
    /// formatting, stylized messages, and rendering components.
    /// </remarks>
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Represents a service responsible for managing application configuration settings.
    /// Used to retrieve, update, or handle configuration-related operations.
    /// </summary>
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Represents the logger for the <see cref="ShowConfigCommand"/> class.
    /// Provides logging functionalities to record execution details, diagnostics, and error handling during the command's operations.
    /// </summary>
    private readonly ILogger<ShowConfigCommand> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowConfigCommand"/> class.
    /// </summary>
    /// <param name="console">The console instance.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger instance.</param>
    public ShowConfigCommand(
        IAnsiConsole console,
        IConfigurationService configurationService,
        ILogger<ShowConfigCommand> logger
    )
    {
        _console = console;
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override int Execute(CommandContext context)
    {
        _logger.LogInformation("Displaying configuration values");

        try
        {
            // Retrieve strongly-typed configuration options and raw configuration values
            var sampleOptions = _configurationService.GetSampleOptions();
            var allConfig = _configurationService.GetAllConfigurationValues();

            // Part 1: Display strongly-typed configuration
            // This demonstrates how YAML configuration is bound to C# objects
            var sampleTable = new Table()
                .Title("[bold blue]Sample Configuration[/]")
                .AddColumn("[bold]Property[/]")
                .AddColumn("[bold]Value[/]")
                .Border(TableBorder.Rounded);

            // Add configuration values with appropriate formatting and colors
            sampleTable.AddRow(
                new Markup(Markup.Escape("Application Name")),
                new Markup($"[green]{Markup.Escape(sampleOptions.ApplicationName)}[/]")
            );
            sampleTable.AddRow(
                new Markup(Markup.Escape("Version")),
                new Markup($"[yellow]{Markup.Escape(sampleOptions.Version)}[/]")
            );

            // Database configuration section
            sampleTable.AddRow(
                new Markup(Markup.Escape("Database Connection")),
                new Markup($"[cyan]{Markup.Escape(sampleOptions.Database.ConnectionString)}[/]")
            );
            sampleTable.AddRow(
                new Markup(Markup.Escape("Database Max Connections")),
                new Markup($"[white]{sampleOptions.Database.MaxConnections}[/]")
            );
            sampleTable.AddRow(
                new Markup(Markup.Escape("Database Timeout")),
                new Markup($"[white]{sampleOptions.Database.CommandTimeoutSeconds}s[/]")
            );

            // API configuration section
            sampleTable.AddRow(
                new Markup(Markup.Escape("API Base URL")),
                new Markup($"[cyan]{Markup.Escape(sampleOptions.Api.BaseUrl)}[/]")
            );
            sampleTable.AddRow(
                new Markup(Markup.Escape("API Timeout")),
                new Markup($"[white]{sampleOptions.Api.TimeoutMs}ms[/]")
            );

            // Feature flags with color coding based on enabled state
            sampleTable.AddRow(
                new Markup(Markup.Escape("Enable Logging")),
                new Markup(
                    $"[{(sampleOptions.Features.EnableLogging ? "green" : "red")}]{sampleOptions.Features.EnableLogging}[/]"
                )
            );
            sampleTable.AddRow(
                new Markup(Markup.Escape("Enable Metrics")),
                new Markup(
                    $"[{(sampleOptions.Features.EnableMetrics ? "green" : "red")}]{sampleOptions.Features.EnableMetrics}[/]"
                )
            );
            sampleTable.AddRow(
                new Markup(Markup.Escape("Enable Debug Mode")),
                new Markup(
                    $"[{(sampleOptions.Features.EnableDebugMode ? "green" : "red")}]{sampleOptions.Features.EnableDebugMode}[/]"
                )
            );

            // Display array values as comma-separated list
            if (sampleOptions.SupportedEnvironments.Count > 0)
            {
                sampleTable.AddRow(
                    new Markup(Markup.Escape("Supported Environments")),
                    new Markup(
                        $"[magenta]{Markup.Escape(string.Join(", ", sampleOptions.SupportedEnvironments))}[/]"
                    )
                );
            }

            _console.Write(sampleTable);

            // Part 2: Display all configuration values in flattened format
            // This shows how YAML is converted to the key-value format used by IConfiguration
            var allConfigTable = new Table()
                .Title("[bold blue]All Configuration Values[/]")
                .AddColumn("[bold]Key[/]")
                .AddColumn("[bold]Value[/]")
                .Border(TableBorder.Rounded);

            // Sort configuration keys for better readability
            foreach (var kvp in allConfig.OrderBy(static x => x.Key))
            {
                // Determine sensitivity and null
                bool isNull = kvp.Value is null;
                bool isSensitive =
                    kvp.Key.Contains("password", StringComparison.OrdinalIgnoreCase)
                    || kvp.Key.Contains("secret", StringComparison.OrdinalIgnoreCase)
                    || kvp.Key.Contains("key", StringComparison.OrdinalIgnoreCase);

                // Escape markup in key and value, except explicit mask or null markers
                var keyCell = new Markup(Markup.Escape(kvp.Key));
                Markup valueCell;
                if (isSensitive)
                {
                    valueCell = new Markup("[dim]****[/]");
                }
                else if (isNull)
                {
                    valueCell = new Markup("[dim]null[/]");
                }
                else
                {
                    valueCell = new Markup(Markup.Escape(kvp.Value!));
                }

                allConfigTable.AddRow(keyCell, valueCell);
            }

            _console.WriteLine();
            _console.Write(allConfigTable);

            _console.WriteLine();
            _console.MarkupLine("[bold green]✓[/] Configuration displayed successfully!");

            return 0;
        }
        catch (Exception ex)
        {
            // Log the error for diagnostics and display user-friendly message
            _logger.LogError(ex, "Error displaying configuration");
            _console.MarkupLine("[bold red]✗[/] Error displaying configuration: {0}", ex.Message);
            return 1;
        }
    }
}
