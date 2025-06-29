// <copyright file="ValidateConfigCommand.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration.Sample.Commands;

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Services;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Command to validate YAML configuration files.
/// This command demonstrates how to:
/// - Parse and validate YAML syntax
/// - Display detailed error messages for invalid YAML
/// - Show parsed configuration structure
/// - Handle various YAML parsing edge cases
/// </summary>
/// <example>
/// Usage examples:
/// dotnet run -- validate --file appsettings.yaml
/// dotnet run -- validate -f examples/complex.yaml
/// </example>
[UsedImplicitly]
public class ValidateConfigCommand : Command<ValidateConfigCommand.Settings>
{
    /// <summary>
    /// Represents the AnsiConsole interface for writing markup, tables, and other formatted output
    /// to the command-line application.
    /// </summary>
    /// <remarks>
    /// This variable is used for interacting with the console, including displaying validation messages,
    /// file information, and other relevant data related to the execution process of the command.
    /// </remarks>
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Represents the service responsible for configuration-related operations,
    /// including validation, retrieval, and processing of configuration data.
    /// Provides methods to validate YAML files, access configuration values, and
    /// retrieve structured options.
    /// </summary>
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Provides logging functionality within the <see cref="ValidateConfigCommand"/> class.
    /// Used to log informational messages, warnings, and errors during the execution of the YAML validation process.
    /// </summary>
    private readonly ILogger<ValidateConfigCommand> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateConfigCommand"/> class.
    /// </summary>
    /// <param name="console">The console instance.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger instance.</param>
    public ValidateConfigCommand(
        IAnsiConsole console,
        IConfigurationService configurationService,
        ILogger<ValidateConfigCommand> logger
    )
    {
        _console = console;
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the validation logic for a YAML configuration file.
    /// </summary>
    /// <param name="context">The command context containing execution metadata.</param>
    /// <param name="settings">The command settings including user-provided arguments, such as the file path to validate.</param>
    /// <returns>
    /// Returns an integer exit code. A value of 0 indicates successful validation, while non-zero values indicate an error in the process.
    /// </returns>
    public override int Execute(CommandContext context, Settings settings)
    {
        _logger.LogInformation("Validating YAML file: {FilePath}", settings.FilePath);

        try
        {
            // Use default file if not specified
            var filePath = settings.FilePath ?? "appsettings.yaml";

            // Validate file exists before attempting to parse
            if (!File.Exists(filePath))
            {
                _console.MarkupLine("[bold red]✗[/] File not found: {0}", filePath);
                return 1;
            }

            _console.MarkupLine("Validating YAML file: [cyan]{0}[/]", filePath);

            // Perform YAML validation using the configuration service
            // This will catch syntax errors, invalid YAML structures, etc.
            var isValid = _configurationService.ValidateYamlFileAsync(filePath).Result;

            if (isValid)
            {
                _console.MarkupLine("[bold green]✓[/] YAML file is valid!");

                // Display file metadata for user reference
                var fileInfo = new FileInfo(filePath);
                var infoTable = new Table()
                    .Title("[bold blue]File Information[/]")
                    .AddColumn("[bold]Property[/]")
                    .AddColumn("[bold]Value[/]")
                    .Border(TableBorder.Rounded);

                infoTable.AddRow("File Path", $"[cyan]{fileInfo.FullName}[/]");
                infoTable.AddRow("File Size", $"[yellow]{fileInfo.Length:N0} bytes[/]");
                infoTable.AddRow(
                    "Last Modified",
                    $"[white]{fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}[/]"
                );

                _console.Write(infoTable);

                // Attempt to parse and display the YAML content structure
                // This helps users verify their configuration is being parsed as expected
                try
                {
                    var parser = new YamlParser();
                    using var fileStream = File.OpenRead(filePath);
                    var parsedData = parser.Parse(fileStream);

                    if (parsedData.Count > 0)
                    {
                        // Create a table showing the flattened configuration keys
                        // This demonstrates how the YAML structure is converted to the flat key-value format
                        // used by Microsoft.Extensions.Configuration
                        var dataTable = new Table()
                            .Title("[bold blue]Parsed Configuration Keys[/]")
                            .AddColumn("[bold]Key[/]")
                            .AddColumn("[bold]Value[/]")
                            .Border(TableBorder.Rounded);

                        // Show first 10 items as a preview
                        foreach (var kvp in parsedData.Take(10))
                        {
                            var value = kvp.Value ?? "[dim]null[/]";

                            // Truncate long values for better display
                            if (value.Length > 50)
                            {
                                value = value.Substring(0, 47) + "...";
                            }

                            dataTable.AddRow(kvp.Key, value);
                        }

                        // Indicate if there are more keys than displayed
                        if (parsedData.Count > 10)
                        {
                            dataTable.AddRow(
                                "[dim]...[/]",
                                $"[dim]({parsedData.Count - 10} more keys)[/]"
                            );
                        }

                        _console.WriteLine();
                        _console.Write(dataTable);
                    }
                }
                catch (Exception ex)
                {
                    // Non-fatal error - validation passed but couldn't display content
                    _console.MarkupLine(
                        "[yellow]⚠[/] Could not display parsed content: {0}",
                        ex.Message
                    );
                }

                return 0;
            }

            // Validation failed - the service should have already displayed error details
            this._console.MarkupLine("[bold red]✗[/] YAML file is invalid!");
            return 1;
        }
        catch (Exception ex)
        {
            // Log unexpected errors for debugging
            _logger.LogError(ex, "Error validating YAML file");
            _console.MarkupLine("[bold red]✗[/] Error validating file: {0}", ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// Settings for the validate command.
    /// Defines the command-line options available for the validate command.
    /// </summary>
    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the file path to validate.
        /// If not specified, defaults to "appsettings.yaml" in the current directory.
        /// Supports both relative and absolute paths.
        /// </summary>
        /// <example>
        /// --file appsettings.yaml
        /// -f /etc/myapp/config.yaml
        /// --file examples/complex.yaml
        /// </example>
        [Description("Path to the YAML file to validate")]
        [CommandOption("-f|--file")]
        [UsedImplicitly]
        public string? FilePath { get; set; }
    }
}
