// <copyright file="ExampleConfigCommand.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration.Sample.Commands;

using System.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Command to load any YAML file and display its flattened configuration values.
/// Useful for inspecting example YAMLs or standalone config files.
/// </summary>
/// <example>
/// dotnet run -- example examples/advanced-types.yaml
/// </example>
[UsedImplicitly]
public class ExampleConfigCommand(IAnsiConsole console) : Command<ExampleConfigCommand.Settings>
{
    /// <summary>
    /// Settings for the example command.
    /// </summary>
    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the file path for the YAML configuration file to be processed.
        /// </summary>
        /// <remarks>
        /// This property represents the path to the YAML file specified as an argument.
        /// It is used to load configuration settings for flattening and displaying.
        /// </remarks>
        [CommandArgument(0, "<file>")]
        public string File { get; set; } = string.Empty;
    }

    /// <summary>
    /// Executes the command, processes the provided YAML file, and displays its configuration
    /// values in a flattened table format using the ANSI console.
    /// </summary>
    /// <param name="context">The command context, providing metadata and services for the command execution.</param>
    /// <param name="settings">The settings passed to the command, containing the file path for the YAML configuration file.</param>
    /// <returns>An integer representing the exit code of the command. Returns 0 for successful execution.</returns>
    public override int Execute(CommandContext context, Settings settings)
    {
        // Build a configuration from the specified YAML file
        var configuration = new ConfigurationBuilder()
            .AddYamlFile(settings.File, optional: false, reloadOnChange: false)
            .Build();

        // Display flattened key/value table
        var table = new Table()
            .Title("[bold]Configuration Dump[/]")
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Key[/]")
            .AddColumn("[bold]Value[/]");

        foreach (var kvp in configuration.AsEnumerable().OrderBy(static x => x.Key))
        {
            var key = Markup.Escape(kvp.Key);
            var value = kvp.Value is null ? "[dim]null[/]" : Markup.Escape(kvp.Value);

            table.AddRow(new Markup(key), new Markup(value));
        }

        console.Write(table);
        return 0;
    }
}
