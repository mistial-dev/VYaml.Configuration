// <copyright file="ConfigurationService.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration.Sample.Services;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Models;

/// <summary>
/// Implementation of configuration service.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<SampleOptions> _sampleOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="sampleOptions">The sample options monitor.</param>
    public ConfigurationService(
        IConfiguration configuration,
        IOptionsMonitor<SampleOptions> sampleOptions
    )
    {
        _configuration = configuration;
        _sampleOptions = sampleOptions;
    }

    /// <inheritdoc/>
    public SampleOptions GetSampleOptions()
    {
        return _sampleOptions.CurrentValue;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateYamlFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            // Try to parse the YAML file using our parser
            var parser = new YamlParser();
            using var fileStream = File.OpenRead(filePath);
            var result = parser.Parse(fileStream);

            // If we got here without exception, the YAML is valid
            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public IDictionary<string, string?> GetAllConfigurationValues()
    {
        var result = new Dictionary<string, string?>();

        foreach (var kvp in _configuration.AsEnumerable())
        {
            result[kvp.Key] = kvp.Value;
        }

        return result;
    }
}
