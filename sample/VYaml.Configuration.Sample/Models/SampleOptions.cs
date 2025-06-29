// <copyright file="SampleOptions.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration.Sample.Models;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Sample configuration options to demonstrate YAML configuration binding.
/// </summary>
public class SampleOptions
{
    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    [Required]
    public string ApplicationName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Gets or sets the database settings.
    /// </summary>
    public DatabaseSettings Database { get; init; } = new();

    /// <summary>
    /// Gets or sets the API settings.
    /// </summary>
    public ApiSettings Api { get; init; } = new();

    /// <summary>
    /// Gets or sets the feature flags.
    /// </summary>
    public FeatureFlags Features { get; init; } = new();

    /// <summary>
    /// Gets or sets the list of supported environments.
    /// </summary>
    public List<string> SupportedEnvironments { get; init; } = [];
}

/// <summary>
/// Database configuration settings.
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of connections.
    /// </summary>
    [Range(1, 1000)]
    public int MaxConnections { get; set; } = 100;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// API configuration settings.
/// </summary>
public class ApiSettings
{
    /// <summary>
    /// Gets or sets the base URL for the API.
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    [Required]
    [MinLength(10)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in milliseconds.
    /// </summary>
    [Range(1000, 60000)]
    public int TimeoutMs { get; set; } = 5000;
}

/// <summary>
/// Feature flags configuration.
/// </summary>
public class FeatureFlags
{
    /// <summary>
    /// Gets or sets a value indicating whether logging is enabled.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether metrics collection is enabled.
    /// </summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether debug mode is enabled.
    /// </summary>
    public bool EnableDebugMode { get; set; } = false;
}
