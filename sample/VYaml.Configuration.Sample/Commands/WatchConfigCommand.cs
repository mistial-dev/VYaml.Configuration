// <copyright file="WatchConfigCommand.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration.Sample.Commands;

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// A command that demonstrates real-time configuration watching using a YAML configuration file.
/// </summary>
/// <remarks>
/// The WatchConfigCommand monitors changes in a YAML configuration file and reacts to updates
/// in real-time. The configuration file is temporarily created in the system's temporary
/// directory for demonstration purposes. Once changes are detected, the file content is reloaded
/// to reflect updates in real-time.
/// </remarks>
[UsedImplicitly]
public class WatchConfigCommand : AsyncCommand<WatchConfigCommand.Settings>
{
    /// <summary>
    /// Represents an instance of <see cref="IAnsiConsole"/> used for rendering console output.
    /// </summary>
    /// <remarks>
    /// This variable is primarily used for writing styled messages, formatting, and interacting
    /// with the console across various tasks executed by the command.
    /// </remarks>
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Provides logging functionalities for the <see cref="WatchConfigCommand"/> class.
    /// </summary>
    /// <remarks>
    /// Used to log informational, warning, and error messages during the execution of the configuration watch process,
    /// including actions such as initializing configuration files, monitoring changes, and cleaning up resources.
    /// </remarks>
    private readonly ILogger<WatchConfigCommand> _logger;

    /// <summary>
    /// Represents a command that monitors changes to a YAML configuration file and logs updates to the console.
    /// </summary>
    public WatchConfigCommand(
        IAnsiConsole console,
        IConfiguration configuration,
        IOptionsMonitor<SampleOptions> optionsMonitor,
        ILogger<WatchConfigCommand> logger
    )
    {
        _console = console;
        _logger = logger;
    }

    /// <summary>
    /// Executes the asynchronous command for watching configuration changes, including creating a temporary
    /// configuration file, monitoring the file for changes, and periodically handling configuration updates.
    /// </summary>
    /// <param name="context">The context in which the command is executed, containing metadata about the command execution.</param>
    /// <param name="settings">The settings provided for the command execution, including customizable options such as the update interval.</param>
    /// <returns>
    /// A task that represents the asynchronous operation, returning an integer exit code indicating
    /// the result of the command execution.
    /// </returns>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        _logger.LogInformation("Starting configuration watch demo");

        var tempFile = Path.Combine(Path.GetTempPath(), $"vyaml-watch-demo-{Guid.NewGuid()}.yaml");
        _logger.LogInformation("Using temporary file: {TempFile}", tempFile);

        try
        {
            await InitializeConfigFile(tempFile);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(tempFile)!)
                .AddYamlFile(Path.GetFileName(tempFile), optional: false, reloadOnChange: true)
                .Build();

            // Run with cancellation using functional approach
            return await RunWithConsoleCancellation(tempFile, configuration, settings.IntervalMs);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                    _logger.LogInformation("Cleaned up temporary file");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary file");
            }
        }
    }

    /// <summary>
    /// Runs a set of operations while handling console cancellation requests.
    /// </summary>
    /// <param name="tempFile">The path to the temporary file being monitored or operated on.</param>
    /// <param name="configuration">The configuration object tracking changes in the YAML file.</param>
    /// <param name="intervalMs">The interval in milliseconds to wait between operations.</param>
    /// <returns>
    /// A task that represents the asynchronous execution of the operations.
    /// The task result contains the exit code indicating the operation's success or failure.
    /// </returns>
    private async Task<int> RunWithConsoleCancellation(
        string tempFile,
        IConfiguration configuration,
        int intervalMs
    )
    {
        // Create an immutable event stream using channels
        var cancellationChannel = Channel.CreateUnbounded<CancellationEvent>(
            new UnboundedChannelOptions { SingleWriter = true, SingleReader = true }
        );

        // Pure function that writes events - no captured state
        ConsoleCancelEventHandler handler = CreatePureEventHandler(cancellationChannel.Writer);

        Console.CancelKeyPress += handler;

        try
        {
            // Run operations with functional composition
            return await RunOperationsWithCancellationStream(
                tempFile,
                configuration,
                intervalMs,
                cancellationChannel.Reader
            );
        }
        finally
        {
            // Clean up in reverse order
            Console.CancelKeyPress -= handler;
            cancellationChannel.Writer.TryComplete();
        }
    }

    // Pure function factory - returns a handler that only writes messages
    /// <summary>
    /// Creates a pure event handler for handling console cancellation events,
    /// ensuring that the event handler only writes cancellation events to the provided channel writer.
    /// </summary>
    /// <param name="writer">The channel writer to which cancellation events will be written.</param>
    /// <returns>A <see cref="ConsoleCancelEventHandler"/> that writes cancellation events to the specified writer.</returns>
    private static ConsoleCancelEventHandler CreatePureEventHandler(
        ChannelWriter<CancellationEvent> writer
    )
    {
        return (sender, args) =>
        {
            args.Cancel = true;
            // Fire and forget - we don't care if this fails
            _ = writer.TryWrite(new CancellationEvent());
        };
    }

    /// <summary>
    /// Executes operations while monitoring for cancellation events from a given stream.
    /// </summary>
    /// <param name="tempFile">The path to the temporary file used for operations.</param>
    /// <param name="configuration">The configuration instance used for accessing application settings.</param>
    /// <param name="intervalMs">The interval in milliseconds between each operation cycle.</param>
    /// <param name="cancellationStream">
    /// A channel reader used to receive cancellation events that may stop the operations.
    /// </param>
    /// <returns>An integer representing the result of the operation, typically used as an exit code.</returns>
    private async Task<int> RunOperationsWithCancellationStream(
        string tempFile,
        IConfiguration configuration,
        int intervalMs,
        ChannelReader<CancellationEvent> cancellationStream
    )
    {
        using var cts = new CancellationTokenSource();

        // Create a task that monitors the cancellation stream
        var monitorTask = MonitorCancellationStream(cancellationStream, cts);

        try
        {
            // Run main operations
            var result = await RunMainOperationsAsync(
                tempFile,
                configuration,
                intervalMs,
                cts.Token
            );

            return result;
        }
        catch (OperationCanceledException)
        {
            _console.WriteLine();
            _console.MarkupLine("[yellow]⚠[/] Demo cancelled by user");
            return 0;
        }
        finally
        {
            // Cancel if not already done
            await cts.CancelAsync();

            // Wait for monitor to complete
            await monitorTask;
        }
    }

    // Pure async function that monitors events and triggers cancellation
    /// <summary>
    /// Asynchronously monitors a channel for cancellation events and triggers the provided <see cref="CancellationTokenSource"/> when an event is received.
    /// </summary>
    /// <param name="reader">The channel reader that provides cancellation events.</param>
    /// <param name="cts">The <see cref="CancellationTokenSource"/> to trigger upon receiving a cancellation event.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task MonitorCancellationStream(
        ChannelReader<CancellationEvent> reader,
        CancellationTokenSource cts
    )
    {
        try
        {
            await foreach (var _ in reader.ReadAllAsync())
            {
                // Received cancellation event
                await cts.CancelAsync();
                break;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the channel is closed
        }
    }

    /// <summary>
    /// Runs the main operations required for the configuration watch functionality.
    /// </summary>
    /// <param name="tempFile">The path to the temporary file used for configuration modification.</param>
    /// <param name="configuration">The configuration instance to be monitored and displayed.</param>
    /// <param name="intervalMs">The time interval in milliseconds for configuration modifications.</param>
    /// <param name="cancellationToken">Token used to observe cancellation requests.</param>
    /// <returns>An integer representing the result of the operation. Returns 0 if the operation completes successfully, or 1 if an error occurs.</returns>
    private async Task<int> RunMainOperationsAsync(
        string tempFile,
        IConfiguration configuration,
        int intervalMs,
        CancellationToken cancellationToken
    )
    {
        // Use functional composition for parallel operations
        var modifierTask = CreateModifierTask(tempFile, intervalMs, cancellationToken);
        var displayTask = CreateDisplayTask(configuration, cancellationToken);

        try
        {
            // Run display operation and wait for completion
            await displayTask;

            _console.WriteLine();
            _console.MarkupLine("[bold green]✓[/] Configuration watch demo completed!");
            return 0;
        }
        catch (OperationCanceledException)
        {
            throw; // Let caller handle
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration watch");
            _console.MarkupLine("[bold red]✗[/] Error: {0}", ex.Message);
            return 1;
        }
        finally
        {
            // Ensure modifier task completes
            await WaitForTaskCompletion(modifierTask);
        }
    }

    // Functional task creators - pure functions that return tasks
    /// <summary>
    /// Creates a task that continuously modifies a configuration file at specified intervals.
    /// </summary>
    /// <param name="tempFile">The path to the temporary configuration file to modify.</param>
    /// <param name="intervalMs">The interval in milliseconds between modifications.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous modification operation.</returns>
    private Task CreateModifierTask(
        string tempFile,
        int intervalMs,
        CancellationToken cancellationToken
    )
    {
        return Task.Run(
            () => ModifyConfigurationLoop(tempFile, intervalMs, cancellationToken),
            cancellationToken
        );
    }

    /// Creates a task responsible for displaying configuration data periodically.
    /// <param name="configuration">
    /// The configuration object containing the data to be displayed.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to signal the task to stop.
    /// </param>
    /// <return>
    /// A task that represents the asynchronous operation to display configuration data.
    /// </return>
    private Task CreateDisplayTask(
        IConfiguration configuration,
        CancellationToken cancellationToken
    )
    {
        return DisplayConfigurationLoop(configuration, cancellationToken);
    }

    /// <summary>
    /// Waits for the completion of a given task and handles any exceptions.
    /// </summary>
    /// <param name="task">The task to wait for completion.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task WaitForTaskCompletion(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
    }

    /// <summary>
    /// Initializes a temporary YAML configuration file with default values.
    /// </summary>
    /// <param name="filePath">The full path to the file where the configuration will be created.</param>
    /// <returns>A task that represents the asynchronous file initialization operation.</returns>
    private async Task InitializeConfigFile(string filePath)
    {
        var yaml = GenerateInitialYaml(DateTime.Now);
        await File.WriteAllTextAsync(filePath, yaml);
        _console.MarkupLine("[green]✓[/] Created temporary configuration file");
    }

    // Pure function for generating YAML
    /// <summary>
    /// Generates the initial YAML string for configuration with the specified timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to include in the generated YAML.</param>
    /// <returns>A YAML string representing the initial configuration structure.</returns>
    private static string GenerateInitialYaml(DateTime timestamp)
    {
        return $"""
            Sample:
              ApplicationName: VYaml Watch Demo
              Version: 1.0.0
              Counter: 0
              LastModified: {timestamp:yyyy-MM-dd HH:mm:ss}
              Status: Initializing
              Features:
                EnableLogging: true
                EnableMetrics: false
                EnableDebugMode: false

            """;
    }

    /// <summary>
    /// Continuously modifies a configuration file at specified intervals until the
    /// provided cancellation token indicates cancellation. The modifications are
    /// applied using a functional state transformation pattern.
    /// </summary>
    /// <param name="filePath">
    /// The file path of the configuration file to modify.
    /// </param>
    /// <param name="intervalMs">
    /// The interval in milliseconds between successive modifications to the file.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe for cancellation requests.
    /// </param>
    /// <remarks>
    /// This method writes updated YAML data generated from a state object to the
    /// specified file. It logs modification information and handles errors
    /// gracefully, excluding cancellation exceptions.
    /// </remarks>
    private async Task ModifyConfigurationLoop(
        string filePath,
        int intervalMs,
        CancellationToken cancellationToken
    )
    {
        var state = new ModificationState();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(intervalMs, cancellationToken);

                // Use functional state transformation
                state = state.Next();
                var yaml = GenerateYaml(state);

                await File.WriteAllTextAsync(filePath, yaml, cancellationToken);
                _logger.LogDebug("Modified configuration (counter: {Counter})", state.Counter);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error modifying configuration file");
            }
        }
    }

    // Pure function for YAML generation
    /// <summary>
    /// Generates a YAML string representation of a given modification state.
    /// </summary>
    /// <param name="state">The current modification state containing information such as version, counter, last modification timestamp, status, and enabled features.</param>
    /// <returns>A YAML-formatted string representing the provided modification state.</returns>
    private static string GenerateYaml(ModificationState state)
    {
        return $"""
            Sample:
              ApplicationName: VYaml Watch Demo
              Version: {state.Version}
              Counter: {state.Counter}
              LastModified: {state.LastModified:yyyy-MM-dd HH:mm:ss.fff}
              Status: {state.Status}
              Features:
                EnableLogging: true
                EnableMetrics: {state.EnableMetrics.ToString().ToLowerInvariant()}
                EnableDebugMode: {state.EnableDebugMode.ToString().ToLowerInvariant()}
            """;
    }

    /// <summary>
    /// Continuously updates a live display of configuration data on the console
    /// until the provided cancellation token is triggered.
    /// </summary>
    /// <param name="configuration">The configuration source used to retrieve and update configuration data.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the display operation.</param>
    /// <returns>A task representing the asynchronous operation of the configuration display loop.</returns>
    private async Task DisplayConfigurationLoop(
        IConfiguration configuration,
        CancellationToken cancellationToken
    )
    {
        await _console
            .Live(GenerateDisplay(configuration))
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ctx.UpdateTarget(GenerateDisplay(configuration));
                    await Task.Delay(100, cancellationToken);
                }
            });
    }

    /// Generates a visual display panel of the current application configuration settings.
    /// This method constructs a Spectre.Console Panel object with configuration information
    /// retrieved from the provided IConfiguration instance. It uses helper methods to read
    /// the configuration values and construct the display table.
    /// The returned panel is used for rendering configuration data in a terminal-friendly format.
    /// Parameters:
    /// configuration: Represents the configuration root used to retrieve application settings.
    /// Returns:
    /// A Spectre.Console.Panel object containing formatted configuration data.
    private Panel GenerateDisplay(IConfiguration configuration)
    {
        var config = ReadConfiguration(configuration);
        var table = CreateDisplayTable(config);
        return CreateDisplayPanel(table);
    }

    // Pure function to read configuration
    /// <summary>
    /// Reads configuration data from the provided <see cref="IConfiguration"/> object
    /// and returns it as a <see cref="ConfigurationData"/> instance.
    /// </summary>
    /// <param name="configuration">The configuration instance containing key-value pairs to be read and mapped.</param>
    /// <returns>A <see cref="ConfigurationData"/> object populated with the configuration values.</returns>
    private static ConfigurationData ReadConfiguration(IConfiguration configuration)
    {
        return new ConfigurationData(
            AppName: configuration["Sample:ApplicationName"] ?? "N/A",
            Version: configuration["Sample:Version"] ?? "N/A",
            Counter: configuration["Sample:Counter"] ?? "0",
            LastModified: configuration["Sample:LastModified"] ?? "Never",
            Status: configuration["Sample:Status"] ?? "Unknown",
            EnableLogging: configuration.GetValue<bool>("Sample:Features:EnableLogging"),
            EnableMetrics: configuration.GetValue<bool>("Sample:Features:EnableMetrics"),
            EnableDebugMode: configuration.GetValue<bool>("Sample:Features:EnableDebugMode")
        );
    }

    // Pure function to create display table
    /// <summary>
    /// Creates a display table based on the provided configuration data.
    /// </summary>
    /// <param name="config">An object containing configuration data to be displayed in the table.</param>
    /// <returns>A table populated with configuration data for display purposes.</returns>
    private static Table CreateDisplayTable(ConfigurationData config)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Application", $"[cyan]{config.AppName}[/]");
        table.AddRow("Version", $"[yellow]{config.Version}[/]");
        table.AddRow("Counter", $"[green]{config.Counter}[/]");
        table.AddRow("Status", $"[magenta]{config.Status}[/]");
        table.AddRow("Last Modified", $"[white]{config.LastModified}[/]");
        table.AddEmptyRow();
        table.AddRow(
            "Enable Logging",
            config.EnableLogging ? "[green]✓ Enabled[/]" : "[red]✗ Disabled[/]"
        );
        table.AddRow(
            "Enable Metrics",
            config.EnableMetrics ? "[green]✓ Enabled[/]" : "[red]✗ Disabled[/]"
        );
        table.AddRow(
            "Enable Debug Mode",
            config.EnableDebugMode ? "[green]✓ Enabled[/]" : "[red]✗ Disabled[/]"
        );

        return table;
    }

    // Pure function to create display panel
    /// <summary>
    /// Creates a display panel using the provided table as its content. The panel is configured
    /// with a header and layout adjustments for styling purposes.
    /// </summary>
    /// <param name="table">The table content to embed within the display panel.</param>
    /// <returns>A configured <see cref="Panel"/> object representing the display panel.</returns>
    private static Panel CreateDisplayPanel(Table table)
    {
        var panel = new Panel(table)
            .Header("[bold blue]Real-time Configuration Monitor[/]")
            .HeaderAlignment(Justify.Center)
            .Padding(1, 1);

        var layout = new Layout("Root").SplitRows(
            new Layout("Content", panel),
            new Layout("Footer", new Markup("[dim]Press Ctrl+C to stop watching[/]")).Size(1)
        );

        return new Panel(layout).NoBorder();
    }

    /// <summary>
    /// Represents the settings configuration for the WatchConfigCommand.
    /// </summary>
    /// <remarks>
    /// This class defines properties that configure the behavior of the WatchConfigCommand.
    /// It includes command-line options for controlling execution parameters.
    /// </remarks>
    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the update interval in milliseconds.
        /// </summary>
        /// <remarks>
        /// This property defines the frequency, in milliseconds, at which the operation is performed
        /// or the configuration is refreshed. A default value of 1000 milliseconds is used if no
        /// specific value is provided.
        /// </remarks>
        [Description("Update interval in milliseconds")]
        [CommandOption("-i|--interval")]
        [DefaultValue(1000)]
        public int IntervalMs { get; set; } = 1000;
    }

    // Immutable value types for functional approach
    /// <summary>
    /// Represents an immutable event used for signaling cancellation in a functional programming approach.
    /// </summary>
    private readonly record struct CancellationEvent;

    /// <summary>
    /// Represents the configuration data structure used to store application settings.
    /// </summary>
    private readonly record struct ConfigurationData(
        string AppName,
        string Version,
        string Counter,
        string LastModified,
        string Status,
        bool EnableLogging,
        bool EnableMetrics,
        bool EnableDebugMode
    );

    // Immutable state with functional transformation
    /// <summary>
    /// Represents an immutable state used to track configuration modifications over time.
    /// The state includes properties such as version, modification counter, status,
    /// last modification timestamp, and feature flags for metrics and debug mode.
    /// </summary>
    private sealed class ModificationState
    {
        /// <summary>
        /// Represents a predefined collection of statuses used to indicate the current state.
        /// </summary>
        /// <remarks>
        /// The statuses include values such as "Running", "Active", "Processing", "Monitoring", and "Watching".
        /// These are used to cycle through different states in the application logic.
        /// </remarks>
        private static readonly string[] Statuses =
        [
            "Running",
            "Active",
            "Processing",
            "Monitoring",
            "Watching",
        ];

        /// <summary>
        /// Represents an immutable counter property that tracks the current state of the configuration modifications.
        /// The counter increments with each transformation of the configuration state.
        /// </summary>
        public int Counter { get; }

        /// <summary>
        /// Represents the current operational status of the system or component.
        /// </summary>
        /// <remarks>
        /// The value of this property is derived from a predefined set of statuses, such as "Running,"
        /// "Active," "Processing," "Monitoring," and "Watching." The status indicates the current
        /// state of operation, which may be updated periodically or during state transitions.
        /// This property is immutable and maintained as part of the functional transformation logic
        /// in the system.
        /// </remarks>
        /// <value>
        /// A string representing the current state or activity of the system.
        /// </value>
        public string Status { get; }

        /// <summary>
        /// Represents the version information within the application state.
        /// This property maintains the current version identifier in the
        /// form of a string, typically following semantic versioning format
        /// (e.g., "1.0.0").
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Represents the date and time when the last modification occurred.
        /// </summary>
        /// <remarks>
        /// The <c>LastModified</c> property provides a timestamp that indicates the most recent update
        /// or change made to the associated data or configuration.
        /// </remarks>
        public DateTime LastModified { get; }

        /// <summary>
        /// Gets a value indicating whether metrics are enabled.
        /// </summary>
        /// <remarks>
        /// This property reflects the state of whether the metrics feature is active or inactive.
        /// It may be toggled based on specific conditions or requirements during the lifecycle of the application.
        /// </remarks>
        public bool EnableMetrics { get; }

        /// <summary>
        /// Specifies whether debug mode is enabled.
        /// When enabled, additional detailed logs or diagnostic information may be displayed or generated
        /// to assist in debugging or monitoring the application's behavior.
        /// </summary>
        public bool EnableDebugMode { get; }

        /// Represents an immutable state used to track and modify configuration settings.
        /// The `ModificationState` class follows a functional approach where each transformation
        /// produces a new instance, ensuring immutability of the state.
        public ModificationState()
            : this(0, Statuses[0], "1.0.0", DateTime.Now, false, false) { }

        /// <summary>
        /// Represents the immutable state for configuration modification with functional transformation capabilities.
        /// </summary>
        private ModificationState(
            int counter,
            string status,
            string version,
            DateTime lastModified,
            bool enableMetrics,
            bool enableDebugMode
        )
        {
            Counter = counter;
            Status = status;
            Version = version;
            LastModified = lastModified;
            EnableMetrics = enableMetrics;
            EnableDebugMode = enableDebugMode;
        }

        // Pure transformation function
        /// Performs a state transformation to generate the next modification state.
        /// The method increments the counter, updates the status, toggles the EnableMetrics
        /// and EnableDebugMode flags based on defined conditions, and modifies the version
        /// string if specific conditions are met. The LastModified property is also updated
        /// to the current timestamp at the time of transformation.
        /// <returns>
        /// A new instance of ModificationState representing the next state with updated
        /// properties based on the transformation logic.
        /// </returns>
        public ModificationState Next()
        {
            var newCounter = Counter + 1;
            var newStatus = Statuses[newCounter % Statuses.Length];
            var newEnableMetrics = newCounter % 3 == 0 ? !EnableMetrics : EnableMetrics;
            var newEnableDebugMode = newCounter % 5 == 0 ? !EnableDebugMode : EnableDebugMode;

            var newVersion = Version;
            if (newCounter % 10 == 0)
            {
                var major = 1 + (newCounter / 100);
                var minor = (newCounter / 10) % 10;
                var patch = newCounter % 10;
                newVersion = $"{major}.{minor}.{patch}";
            }

            return new ModificationState(
                newCounter,
                newStatus,
                newVersion,
                DateTime.Now,
                newEnableMetrics,
                newEnableDebugMode
            );
        }
    }
}
