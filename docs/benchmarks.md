# Benchmarks

This document describes how to run and compare benchmarks for VYaml.Configuration.

## Parameter Sweep

The benchmarks accept a `ScaleFactor` parameter (`100`, `1000`, `10000`) and regenerate test data at runtime based on this value. This allows charting throughput vs. configuration size.

## Cross-Framework Comparison

To spot improvements in the base class libraries and runtime, the benchmarks are multi-targeted for both `net8.0` and `net9.0`. Compare results across frameworks by running:

```bash
# Run benchmarks for net8.0 and net9.0
for framework in net8.0 net9.0; do
  dotnet run -c Release -f "$framework" --project benchmarks/VYaml.Configuration.Benchmarks
done
```

## Parser Comparisons

The benchmarks compare three modes for both parsers:
- **Direct stream parsing** (no file I/O, no builder overhead)
- **Builder-based load (no reload)**
- **Builder-based load with reload-on-change**

Specifically:
```csharp
// 1. Direct in-memory stream parsing (pure parser cost)
LoadYamlFile()
LoadYamlFile_NetEscapades_Memory()

// 2. File-based builder load (no file-watch)
LoadYamlFile_NetEscapades()

// 3. File-based builder load with reload-on-change
CreateBuilderWithReloadOnChange()
CreateBuilderWithReloadOnChange_NetEscapades()
```

## Running Benchmarks

```bash
# Run a full sweep of all scales, shapes, and parsers (15 runs) with memory diagnostics:
for framework in net8.0 net9.0; do
  sudo dotnet run -c Release -f "$framework" --project benchmarks/VYaml.Configuration.Benchmarks
done
```
