// <copyright file="YamlConfigurationBenchmarks.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>
namespace VYaml.Configuration.Benchmarks;

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using YamlConfigurationExtensions = YamlConfigurationExtensions;

/// <summary>
/// Benchmarks for evaluating performance of YAML and JSON configuration loading mechanisms.
/// </summary>
[MemoryDiagnoser]
public class YamlConfigurationBenchmarks
{
    /// <summary>
    /// Specifies the directory name where test data files, such as YAML
    /// and JSON files, are stored. This directory is utilized as the
    /// base location for loading and generating file-based test data in
    /// benchmark operations.
    /// </summary>
    private const string TestDataDir = "TestData";

    /// <summary>
    /// Stores the byte array content of a specific YAML test data file,
    /// which is loaded from the TestData directory based on the specified
    /// file name and used for performance benchmarking scenarios.
    /// </summary>
    private byte[] yamlBytes = null!;

    /// <summary>
    /// Stores the byte array content of a generated JSON configuration file,
    /// which is created dynamically based on the specified scale factor
    /// and used in performance benchmarks for JSON data processing.
    /// </summary>
    private byte[] jsonBytes = null!;

    /// <summary>
    /// Represents a byte array containing the encoded representation of a small YAML file.
    /// This data is used for performance benchmarking and testing purposes when loading small
    /// YAML configurations.
    /// </summary>

    /// <summary>
    /// Gets or sets the scale factor for generating YAML test data files used in benchmarking.
    /// </summary>
    [Params(100, 500, 1000)]
    [UsedImplicitly]
    public int ScaleFactor { get; set; }

    /// <summary>
    /// Gets or sets the name of the test data file to load in benchmarks
    /// (e.g. "small.yaml", "large.yaml", "nested.yaml", "arrays.yaml", "complex.yaml").
    /// </summary>
    [Params("small.yaml", "large.yaml", "nested.yaml", "arrays.yaml", "complex.yaml")]
    [PublicAPI]
    public string TestDataFile { get; set; } = null!;

    /// <summary>
    /// Prepares the necessary setup for benchmarking YAML and JSON configuration loading.
    /// </summary>
    /// <remarks>
    /// Regenerates YAML test data files for the current <see cref="ScaleFactor"/>, and initializes
    /// byte arrays containing YAML and JSON data used by benchmarking methods to measure performance.
    /// </remarks>
    [GlobalSetup]
    public void Setup()
    {
        var testDataDir = Path.Combine(AppContext.BaseDirectory, TestDataDir);

        var script = Path.Combine(testDataDir, "GenerateYamlTestFiles.py");
        // Scale nested depth and width more conservatively to prevent exponential growth
        var nestedDepth = Math.Min(10, 3 + (this.ScaleFactor / 1000)); // Max depth of 10
        var nestedWidth = Math.Min(20, 5 + (this.ScaleFactor / 200)); // Max width of 20
        var arrayProps = Math.Min(10, 5 + (this.ScaleFactor / 1000)); // Max 10 properties per array item

        var psi = new ProcessStartInfo
        {
            FileName = "python3",
            Arguments =
                $"\"{script}\" --small-size {this.ScaleFactor} --large-size {this.ScaleFactor} "
                + $"--nested-depth {nestedDepth} --nested-width {nestedWidth} "
                + $"--array-size {this.ScaleFactor} --array-props {arrayProps} --output-dir \"{testDataDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var scriptProcess = Process.Start(psi)!;
        scriptProcess.WaitForExit();
        if (scriptProcess.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Test data generation failed: {scriptProcess.StandardError.ReadToEnd()}"
            );
        }

        this.yamlBytes = File.ReadAllBytes(Path.Combine(testDataDir, this.TestDataFile));
        this.jsonBytes = Encoding.UTF8.GetBytes(GenerateJson(this.ScaleFactor));
    }

    /// <summary>
    /// Measures the performance of loading a small YAML file into a configuration provider.
    /// </summary>
    /// <remarks>
    /// This method uses the <see cref="YamlConfigurationProvider"/> to load a YAML configuration
    /// file represented as a byte array. It benchmarks the loading process for small YAML files.
    /// </remarks>
    /// <exception cref="FormatException">
    /// Thrown when the YAML content being loaded is invalid.
    /// </exception>
    [Benchmark]
    public void LoadYamlFile()
    {
        var provider = new YamlConfigurationProvider(
            new YamlConfigurationSource { Optional = false, ReloadOnChange = false }
        );
        using var stream = new MemoryStream(this.yamlBytes, writable: false);
        provider.Load(stream);
    }

    /// <summary>
    /// Reads and processes configuration data from a JSON file.
    /// </summary>
    /// <remarks>
    /// This method uses a <see cref="JsonConfigurationProvider"/> to load JSON data
    /// from a memory stream created with the provided byte array.
    /// </remarks>
    [Benchmark(Baseline = true)]
    public void LoadJsonFile()
    {
        var provider = new JsonConfigurationProvider(
            new JsonConfigurationSource { Optional = false, ReloadOnChange = false }
        );
        using var stream = new MemoryStream(this.jsonBytes, writable: false);
        provider.Load(stream);
    }

    /// <summary>
    /// Measures the performance of loading a YAML configuration file using the NetEscapades.Extensions.Configuration.Yaml library.
    /// </summary>
    /// <remarks>
    /// This benchmark utilizes the `AddYamlFile` method provided by the NetEscapades.Extensions.Configuration.Yaml library
    /// to load a specified YAML configuration file from the `TestData` directory.
    /// The method evaluates the build time of the configuration for performance analysis.
    /// </remarks>
    [Benchmark]
    public void LoadYamlFile_NetEscapades()
    {
        var builder = new ConfigurationBuilder();
        YamlConfigurationExtensions.AddYamlFile(
            builder,
            Path.Combine(TestDataDir, this.TestDataFile),
            optional: false,
            reloadOnChange: false
        );
        builder.Build();
    }

    /// <summary>
    /// Directly measures parsing and flattening cost for YamlDotNet via the NetEscapades provider
    /// using an in-memory stream (no file I/O or builder overhead).
    /// </summary>
    [Benchmark]
    public void LoadYamlFile_NetEscapades_Memory()
    {
        var source = new NetEscapades.Configuration.Yaml.YamlConfigurationSource
        {
            Optional = false,
            ReloadOnChange = false,
        };
        var provider = new NetEscapades.Configuration.Yaml.YamlConfigurationProvider(source);
        using var stream = new MemoryStream(this.yamlBytes, writable: false);
        provider.Load(stream);
    }

    /// <summary>
    /// Creates and configures a new <see cref="ConfigurationBuilder"/> to load a YAML configuration file with the reload-on-change feature enabled.
    /// </summary>
    /// <remarks>
    /// Utilizes <see cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder,string,bool,bool)"/>
    /// to include the specified YAML configuration file in the builder with reload-on-change enabled
    /// and calls Build() to initialize the provider.
    /// </remarks>
    [Benchmark]
    public void CreateBuilderWithReloadOnChange()
    {
        var builder = new ConfigurationBuilder();
        YamlConfigurationExtensions.AddYamlFile(
            builder,
            Path.Combine(TestDataDir, this.TestDataFile),
            optional: false,
            reloadOnChange: true
        );
        builder.Build();
    }

    /// <summary>
    /// Creates a configuration builder and adds a YAML configuration source with reload-on-change enabled.
    /// </summary>
    /// <remarks>
    /// Utilizes the NetEscapades YAML configuration extension to add a YAML configuration file as a source
    /// to the builder. This benchmark measures the performance of constructing the builder and loading the
    /// configuration with automatic reloading enabled upon file changes.
    /// </remarks>
    [Benchmark]
    public void CreateBuilderWithReloadOnChange_NetEscapades()
    {
        var builder = new ConfigurationBuilder();
        YamlConfigurationExtensions.AddYamlFile(
            builder,
            Path.Combine("TestData", this.TestDataFile),
            optional: false,
            reloadOnChange: true
        );
        builder.Build();
    }

    /// <summary>
    /// Generates a JSON string with a specified number of key-value pairs.
    /// </summary>
    /// <param name="count">The number of key-value pairs to include in the generated JSON.</param>
    /// <returns>A JSON formatted string containing the specified number of key-value pairs.</returns>
    private static string GenerateJson(int count)
    {
        var sb = new StringBuilder();
        sb.Append('{');
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append('"')
                .Append("key")
                .Append(i)
                .Append('"')
                .Append(':')
                .Append('"')
                .Append("value")
                .Append(i)
                .Append('"');

        }

        sb.Append('}');
        return sb.ToString();
    }
}
