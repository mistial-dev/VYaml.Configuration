// <copyright file="Program.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>
namespace VYaml.Configuration.Benchmarks;

using System.IO;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

/// <summary>
/// Entry point of the program that triggers the benchmark execution.
/// </summary>
/// <remarks>
/// The <c>Program</c> class is defined as a static class containing the <c>Main</c> method.
/// This method initializes and executes the benchmarks for YAML configuration processing using BenchmarkDotNet.
/// </remarks>
public static class Program
{
    /// <summary>
    /// Entry point of the program that triggers the benchmark execution.
    /// </summary>
    /// <param name="args">An array of command-line arguments passed to the program.</param>
    public static void Main(string[] args)
    {
        // Include the target framework in the artifacts path to prevent clobbering
        var targetFramework =
#if NET9_0
            "net9"
#elif NET8_0
            "net8"
#else
            "netstandard2"
#endif
        ;

        var config = DefaultConfig
            .Instance.WithArtifactsPath(Path.Combine("..", "artifacts", targetFramework))
            .AddJob(Job.ShortRun);

        BenchmarkRunner.Run<YamlConfigurationBenchmarks>(config);
    }
}
