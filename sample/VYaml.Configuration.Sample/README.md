# VYaml.Configuration Sample Application

This comprehensive sample application demonstrates how to use the VYaml.Configuration library for YAML-based configuration in .NET applications. It showcases all major features and best practices for integrating YAML configuration with Microsoft.Extensions.Configuration.

## 📋 Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Features Demonstrated](#features-demonstrated)
- [Configuration Examples](#configuration-examples)
- [Running the Application](#running-the-application)
- [Commands Available](#commands-available)
- [Advanced Scenarios](#advanced-scenarios)
- [Best Practices](#best-practices)

## 🎯 Overview

This sample application is a command-line tool that demonstrates:

- ✅ Loading YAML configuration files with `Microsoft.Extensions.Configuration`
- ✅ Binding YAML configuration to strongly-typed options classes
- ✅ Support for nested configuration structures
- ✅ Array and list configuration
- ✅ Environment-specific configuration files
- ✅ Configuration validation with data annotations
- ✅ Real-time configuration reloading
- ✅ Integration with dependency injection
- ✅ Environment variable substitution
- ✅ Multi-line string support (literal and folded)

## 📦 Prerequisites

- .NET 8.0 or .NET 9.0 SDK
- Visual Studio 2022, VS Code, or your preferred IDE
- Basic knowledge of .NET configuration system

## 🚀 Getting Started

1. **Navigate to the sample directory:**
   ```bash
   cd sample/VYaml.Configuration.Sample
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the application:**
   ```bash
   dotnet build
   ```

4. **Run the application:**
   ```bash
   dotnet run -- show
   ```

## 📁 Project Structure

```
VYaml.Configuration.Sample/
├── Commands/                    # CLI command implementations
│   ├── ShowConfigCommand.cs    # Displays current configuration
│   └── ValidateConfigCommand.cs # Validates YAML files
├── Models/                      # Configuration model classes
│   └── SampleOptions.cs        # Strongly-typed configuration models
├── Services/                    # Application services
│   ├── IConfigurationService.cs # Configuration service interface
│   └── ConfigurationService.cs  # Configuration service implementation
├── appsettings.yaml            # Main configuration file
├── appsettings.Development.yaml # Environment-specific config
├── Program.cs                  # Application entry point
├── TypeRegistrar.cs            # DI container for Spectre.Console
└── TypeResolver.cs             # DI resolver for Spectre.Console
```

## 🌟 Features Demonstrated

### 1. Basic Configuration Loading

```csharp
var configuration = new ConfigurationBuilder()
    .AddYamlFile("appsettings.yaml", optional: false, reloadOnChange: true)
    .AddYamlFile($"appsettings.{environment}.yaml", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();
```

### 2. Strongly-Typed Options Binding

The sample uses the Options pattern to bind YAML configuration to C# classes:

```yaml
Sample:
  ApplicationName: "VYaml Configuration Sample"
  Version: "1.0.0"
  Database:
    ConnectionString: "Server=localhost;Database=SampleDb"
    MaxConnections: 50
```

Binds to:

```csharp
public class SampleOptions
{
    public string ApplicationName { get; set; }
    public string Version { get; set; }
    public DatabaseSettings Database { get; set; }
}
```

### 3. Configuration Validation

The sample demonstrates data annotations for configuration validation:

```csharp
public class ApiSettings
{
    [Required]
    [Url]
    public string BaseUrl { get; set; }
    
    [Required]
    [MinLength(10)]
    public string ApiKey { get; set; }
    
    [Range(1000, 60000)]
    public int TimeoutMs { get; set; }
}
```

### 4. Arrays and Lists

```yaml
SupportedEnvironments:
  - Development
  - Staging
  - Production
```

### 5. Complex Nested Structures

```yaml
Services:
  EmailService:
    Provider: "SendGrid"
    Settings:
      ApiKey: "${EMAIL_API_KEY}"
      Templates:
        Welcome: "d-123456789"
        PasswordReset: "d-987654321"
```

## 📝 Configuration Examples

### appsettings.yaml

The main configuration file demonstrates various YAML features:

```yaml
# Simple key-value pairs
Debug: true
Port: 8080

# Nested configuration
Database:
  ConnectionString: "Server=localhost;Database=MyApp"
  MaxConnections: 50

# Arrays
SupportedEnvironments:
  - Development
  - Staging
  - Production

# Multi-line strings (literal style - preserves line breaks)
Documentation: |
  This is a multi-line string that preserves
  line breaks and formatting exactly as written.

# Folded strings (converts line breaks to spaces)
Description: >
  This is a folded string where line breaks
  are converted to spaces, ideal for long descriptions.

# Environment variable placeholders
Services:
  EmailService:
    ApiKey: "${EMAIL_API_KEY}"
```

### appsettings.Development.yaml

Environment-specific overrides:

```yaml
Sample:
  Features:
    EnableDebugMode: true
    EnableMetrics: true

Logging:
  LogLevel:
    Default: Debug
    Microsoft: Information

# Development-specific connection strings
ConnectionStrings:
  DefaultConnection: "Server=(localdb)\\mssqllocaldb;Database=DevDb"
```

## 🎮 Running the Application

## 🛠 Commands Available

Below is a list of CLI commands provided by this sample application:

### Show Configuration Command

Display all loaded configuration values:

```bash
dotnet run -- show
```

This command:
- Loads configuration from all sources
- Displays configuration in formatted tables
- Masks sensitive values (passwords, keys, secrets)
- Shows both strongly-typed options and raw key-value pairs

### Validate Configuration Command

Validate a YAML configuration file:

```bash
dotnet run -- validate --file appsettings.yaml
```

This command:
- Parses the specified YAML file
- Reports any syntax errors with line/column information
- Validates the structure and data types
- Provides helpful error messages

### Watch Configuration Command

Demonstrate real-time configuration updates with multi-threading:

```bash
dotnet run -- watch
```

Or with a custom update interval:

```bash
dotnet run -- watch --interval 2000
```

This command:
- Creates a temporary YAML configuration file
- Starts a background thread that continuously modifies the configuration
- Displays current configuration values in real-time as they change
- Demonstrates the `reloadOnChange` feature of VYaml.Configuration
- Shows how to use `IOptionsMonitor<T>` for change notifications
- Press Ctrl+C to stop the demo

Features demonstrated:
- Multi-threaded configuration updates
- Real-time configuration reloading
- Live display updates using Spectre.Console
- Graceful shutdown handling
- Temporary file management

### Example Command

Inspect a standalone YAML file and display its flattened key/value pairs:

```bash
dotnet run -- example examples/advanced-types.yaml
```

## 🔧 Advanced Scenarios

### 1. Real-time Configuration Reloading

The sample is configured with `reloadOnChange: true`, which means:
- Configuration automatically reloads when YAML files change
- No application restart required
- Use `IOptionsMonitor<T>` to get updated values

### 2. Environment Variable Substitution

While not built into VYaml.Configuration, you can implement substitution:

```yaml
Api:
  Key: "${API_KEY:-default-key}"  # Use API_KEY env var or default
```

### 3. Configuration Sources Priority

Configuration sources are loaded in order, with later sources overriding earlier ones:

1. `appsettings.yaml` (base configuration)
2. `appsettings.{Environment}.yaml` (environment-specific)
3. Environment variables (highest priority)

### 4. Custom Configuration Validation

```csharp
services.PostConfigure<SampleOptions>(options =>
{
    if (options.Database.MaxConnections < 10)
    {
        throw new OptionsValidationException(
            nameof(SampleOptions),
            typeof(SampleOptions),
            new[] { "MaxConnections must be at least 10" });
    }
});
```

## 💡 Best Practices

### 1. File Organization

- Keep base configuration in `appsettings.yaml`
- Use environment-specific files for overrides
- Group related settings in nested sections
- Use meaningful section names

### 2. Security

- Never commit sensitive data to source control
- Use environment variables for secrets
- Consider using Secret Manager for development
- Use Key Vault or similar for production

### 3. Validation

- Always validate configuration at startup
- Use data annotations for basic validation
- Implement `IValidateOptions<T>` for complex validation
- Fail fast with clear error messages

### 4. Documentation

- Comment your YAML files
- Document required vs optional settings
- Provide example values
- Explain the purpose of each section

### 5. Testing

- Test configuration loading in unit tests
- Verify environment-specific overrides work
- Test configuration validation rules
- Test error scenarios (missing files, invalid syntax)

## 🔍 Troubleshooting

### Common Issues

1. **File Not Found**
   - Ensure YAML files are copied to output directory
   - Check file paths are correct
   - Verify working directory

2. **Invalid YAML Syntax**
   - Use spaces, not tabs
   - Check indentation is consistent
   - Validate quotes are balanced
   - Use a YAML linter

3. **Configuration Not Binding**
   - Ensure property names match exactly (case-sensitive)
   - Check nested structure matches C# classes
   - Verify types are compatible

4. **Environment Variables Not Working**
   - Ensure `AddEnvironmentVariables()` is called
   - Use correct naming convention (e.g., `Sample__Database__ConnectionString`)
   - Check environment variable is set

## 📚 Additional Resources

- [VYaml.Configuration Documentation](../../README.md)
- [Microsoft.Extensions.Configuration](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [YAML 1.2 Specification](https://yaml.org/spec/1.2/spec.html)
- [Options Pattern in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/options)

## 🤝 Contributing

Feel free to enhance this sample by:
- Adding more configuration scenarios
- Demonstrating additional YAML features
- Adding more validation examples
- Improving error handling

Submit pull requests to the main VYaml.Configuration repository.