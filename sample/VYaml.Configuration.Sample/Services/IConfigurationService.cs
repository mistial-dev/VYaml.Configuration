// <copyright file="IConfigurationService.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration.Sample.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using Models;

/// <summary>
/// Service for working with configuration data.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the sample configuration options.
    /// </summary>
    /// <returns>The sample options.</returns>
    SampleOptions GetSampleOptions();

    /// <summary>
    /// Validates a YAML configuration file.
    /// </summary>
    /// <param name="filePath">The path to the YAML file.</param>
    /// <returns>True if the file is valid, false otherwise.</returns>
    Task<bool> ValidateYamlFileAsync(string filePath);

    /// <summary>
    /// Gets all configuration keys and values.
    /// </summary>
    /// <returns>A dictionary of configuration keys and values.</returns>
    IDictionary<string, string?> GetAllConfigurationValues();
}
