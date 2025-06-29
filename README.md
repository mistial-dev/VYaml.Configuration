# VYaml.Configuration

[![NuGet](https://img.shields.io/nuget/v/VYaml.Configuration.svg?style=flat-square)](https://www.nuget.org/packages/VYaml.Configuration/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/VYaml.Configuration.svg?style=flat-square)](https://www.nuget.org/packages/VYaml.Configuration/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/mistial-dev/VYaml.Configuration/ci-cd.yml?branch=master&style=flat-square)](https://github.com/mistial-dev/VYaml.Configuration/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**High-performance YAML configuration for .NET** — built on [VYaml](https://github.com/hadashiA/VYaml)'s streaming parser with extensive YAML 1.2 support

VYaml.Configuration seamlessly integrates YAML files into .NET's configuration pipeline, enabling human-readable configuration that works exactly like the built-in JSON provider. Perfect for microservices, CLI tools, and any application requiring intuitive configuration management.

## Quick Start

**Install the package:**

```bash
# .NET CLI
dotnet add package VYaml.Configuration

# Package Manager
Install-Package VYaml.Configuration

# PackageReference
<PackageReference Include="VYaml.Configuration" Version="1.0.0" />
```

**Add YAML configuration in 30 seconds:**

```csharp
// Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Add your YAML configuration file
builder.Configuration.AddYamlFile("appsettings.yaml", optional: false);

var app = builder.Build();

// Access configuration values immediately
var serverUrl = app.Configuration["server:url"];
Console.WriteLine($"Server configured at: {serverUrl}");
```

**Your `appsettings.yaml`:**

```yaml
# Server configuration
server:
  url: https://api.example.com
  timeout: 30
  retries: 3
```

## Drop-in Replacement

VYaml.Configuration works exactly like the built-in JSON configuration provider:

```csharp
// Before - JSON configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// After - YAML configuration  
builder.Configuration.AddYamlFile("appsettings.yaml", optional: false, reloadOnChange: true);
```

All features work identically:
- Same configuration path syntax (`section:subsection:value`)
- Same binding to POCO objects
- Same environment-specific file patterns
- Same reload-on-change support
- Same integration with Options pattern

## Why VYaml.Configuration?

**High-performance streaming parser** — Built on VYaml for efficient YAML processing  
**Drop-in replacement** — Works exactly like the built-in JSON configuration provider  
**Extensive YAML 1.2 compliance** — Support for almost all YAML features
**Hot reload support** — Automatic configuration updates without application restart  
**Type-safe binding** — Seamless integration with .NET's Options pattern and validation  

## Features

### Core Capabilities

- **Hierarchical configuration** with dot-notation access (`server:database:connectionString`)
- **Array and sequence support** with automatic index mapping
- **Environment-specific files** (`appsettings.{Environment}.yaml`)
- **Configuration composition** from multiple sources with proper precedence
- **Change notifications** via `IOptionsMonitor<T>`
- **Validation integration** with data annotations and custom validators

## Comprehensive Usage

### Configuration Binding

**Strongly-typed configuration with validation:**

```csharp
// ServerOptions.cs - Define your configuration model
public class ServerOptions
{
    // URL must be provided and valid
    [Required]
    [Url]
    public string Url { get; set; }
    
    // Timeout between 1 and 300 seconds
    [Range(1, 300)]
    public int Timeout { get; set; } = 30;
    
    // Maximum retry attempts
    [Range(0, 10)]
    public int Retries { get; set; } = 3;
}

// Program.cs - Register with DI container
builder.Services.Configure<ServerOptions>(
    builder.Configuration.GetSection("server"))
    .AddOptionsWithValidateOnStart<ServerOptions>();

// ApiService.cs - Consume in your services
public class ApiService
{
    private readonly ServerOptions _options;
    
    // Options injected by DI container
    public ApiService(IOptions<ServerOptions> options)
    {
        _options = options.Value;
    }
    
    // Use configuration in your methods
    public async Task<string> CallApiAsync()
    {
        // Access strongly-typed configuration
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(_options.Timeout) };
        return await client.GetStringAsync(_options.Url);
    }
}
```

### Complex Hierarchical Configuration

```yaml
application:
  name: MyService
  version: 2.0.0
  
features:
  authentication:
    providers:
      - name: AzureAD
        enabled: true
        clientId: ${AZURE_CLIENT_ID}  # Environment variable substitution
      - name: Google
        enabled: false
  rateLimit:
    requests: 100
    window: 60

database:
  connections:
    primary:
      server: localhost
      port: 5432
      database: myapp
    readonly:
      server: localhost-replica
      port: 5432
      database: myapp
```

**Access nested configuration:**

```csharp
// Program.cs - Multiple ways to access configuration

// Method 1: Bind to strongly-typed list
var authProviders = Configuration
    .GetSection("features:authentication:providers")
    .Get<List<AuthProvider>>();

// Method 2: Register entire section with DI
services.Configure<DatabaseOptions>(
    Configuration.GetSection("database"));

// Method 3: Direct access for simple values
var rateLimitRequests = Configuration.GetValue<int>("features:rateLimit:requests");
```

### Environment-Specific Configuration

```csharp
// Program.cs - Layer configuration files by environment
builder.Configuration
    // Base configuration - always loaded
    .AddYamlFile("appsettings.yaml", optional: false, reloadOnChange: true)
    // Environment-specific overrides (Development, Staging, Production)
    .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", 
                 optional: true, reloadOnChange: true)
    // Local developer overrides - not in source control
    .AddYamlFile("appsettings.local.yaml", 
                 optional: true, reloadOnChange: true);
```

### Integration with Other Providers

VYaml.Configuration works seamlessly with all other configuration providers:

```csharp
// Program.cs - Combine multiple configuration sources
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    // 1. Start with JSON base configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    
    // 2. Layer YAML configuration (overrides JSON)
    .AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: true)
    .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", optional: true)
    
    // 3. User secrets for development
    .AddUserSecrets<Program>(optional: true)
    
    // 4. Environment variables
    .AddEnvironmentVariables()
    
    // 5. Command line arguments (highest priority)
    .AddCommandLine(args);

// Configuration is merged in order - later sources override earlier ones
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
```

**Priority order (highest to lowest):**
1. Command line arguments
2. Environment variables
3. User secrets (Development only)
4. `appsettings.{Environment}.yaml`
5. `appsettings.yaml`
6. `appsettings.{Environment}.json`
7. `appsettings.json`

### Configuration Validation

VYaml.Configuration provides comprehensive validation support through the Options pattern:

```csharp
// Models/AppConfiguration.cs - Define your configuration models
public class AppConfiguration
{
    [Required]
    public DatabaseSettings Database { get; set; }
    
    [Required]
    public ApiSettings Api { get; set; }
}

public class DatabaseSettings : IValidatableObject
{
    [Required]
    public string ConnectionString { get; set; }
    
    [Range(1, 100)]
    public int MaxConnections { get; set; } = 10;
    
    [Range(1, 300)]
    public int CommandTimeout { get; set; } = 30;
    
    // Custom validation logic
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ConnectionString?.Contains("Production") == true && MaxConnections < 20)
        {
            yield return new ValidationResult(
                "Production database requires at least 20 connections",
                new[] { nameof(MaxConnections) });
        }
    }
}

public class ApiSettings
{
    [Required, Url]
    public string BaseUrl { get; set; }
    
    [Required]
    public string ApiKey { get; set; }
    
    [EmailAddress]
    public string SupportEmail { get; set; }
}

// Program.cs - Configure validation
builder.Services
    // Basic registration with validation
    .Configure<AppConfiguration>(builder.Configuration)
    .ValidateDataAnnotations()
    
    // Validate on startup (fail fast)
    .ValidateOnStart();

// Advanced validation with custom validators
builder.Services.AddSingleton<IValidateOptions<ApiSettings>, ApiSettingsValidator>();

// Validators/ApiSettingsValidator.cs
public class ApiSettingsValidator : IValidateOptions<ApiSettings>
{
    public ValidateOptionsResult Validate(string name, ApiSettings options)
    {
        if (options.BaseUrl.StartsWith("https://") && !options.BaseUrl.Contains("localhost"))
        {
            return ValidateOptionsResult.Fail("API must use HTTPS in production");
        }
        
        if (options.ApiKey.Length < 32)
        {
            return ValidateOptionsResult.Fail("API key must be at least 32 characters");
        }
        
        return ValidateOptionsResult.Success;
    }
}
```

**Validation at different stages:**

```csharp
// 1. Startup validation (recommended)
builder.Services.AddOptionsWithValidateOnStart<AppConfiguration>()
    .Bind(builder.Configuration);

// 2. First-use validation
public class MyService
{
    public MyService(IOptions<AppConfiguration> options)
    {
        // Throws if validation fails on first access
        var config = options.Value;
    }
}

// 3. Runtime validation with monitoring
public class DynamicService
{
    public DynamicService(IOptionsMonitor<AppConfiguration> monitor)
    {
        monitor.OnChange(config =>
        {
            // Validation runs automatically on configuration changes
            Console.WriteLine("Configuration updated and validated");
        });
    }
}
```

**Example configuration file with validation:**

```yaml
database:
  connectionString: "Server=localhost;Database=myapp;Integrated Security=true"
  maxConnections: 50      # Must be 1-100
  commandTimeout: 30      # Must be 1-300 seconds

api:
  baseUrl: https://api.example.com  # Must be valid URL with HTTPS
  apiKey: ${API_KEY}                # Loaded from environment, must be 32+ chars
  supportEmail: support@example.com # Must be valid email

email:
  from: noreply@example.com
  smtpHost: smtp.example.com
  smtpPort: 587
  allowInsecure: false
```

### Real-time Configuration Updates

```csharp
// DynamicService.cs - React to configuration changes
public class DynamicService
{
    private readonly IOptionsMonitor<FeatureFlags> _features;
    private readonly ILogger<DynamicService> _logger;
    
    public DynamicService(
        IOptionsMonitor<FeatureFlags> features,
        ILogger<DynamicService> logger)
    {
        _features = features;
        _logger = logger;
        
        // Subscribe to configuration changes
        features.OnChange(flags => 
        {
            _logger.LogInformation("Feature flags updated at {Time}", DateTime.UtcNow);
            // Handle the configuration change
            RefreshInternalState(flags);
        });
    }
    
    // Always uses the latest configuration
    public bool IsFeatureEnabled(string feature)
    {
        return _features.CurrentValue.IsEnabled(feature);
    }
    
    private void RefreshInternalState(FeatureFlags flags)
    {
        // Update any cached state based on new configuration
    }
}
```

## Configuration Patterns

### Multi-file Layering

```csharp
// Program.cs - Layer configuration from multiple sources
builder.Configuration
    // System-wide configuration
    .AddYamlFile("/etc/myapp/config.yaml", optional: true)
    // User-specific overrides
    .AddYamlFile($"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.myapp/config.yaml", optional: true)
    // Local development overrides
    .AddYamlFile("config.local.yaml", optional: true)
    // Environment variables take precedence
    .AddEnvironmentVariables("MYAPP_");
```

### Advanced Validation Patterns

```csharp
// FluentValidationOptionsValidator.cs - Using FluentValidation
public class EmailOptionsValidator : AbstractValidator<EmailOptions>, IValidateOptions<EmailOptions>
{
    public EmailOptionsValidator()
    {
        RuleFor(x => x.From).NotEmpty().EmailAddress();
        RuleFor(x => x.SmtpHost).NotEmpty();
        RuleFor(x => x.SmtpPort).InclusiveBetween(1, 65535);
        
        // Complex conditional validation
        When(x => x.SmtpPort == 25, () =>
        {
            RuleFor(x => x.AllowInsecure)
                .Equal(true)
                .WithMessage("Port 25 requires AllowInsecure=true");
        });
        
        When(x => x.SmtpHost.Contains("internal"), () =>
        {
            RuleFor(x => x.SmtpPort)
                .Equal(25)
                .WithMessage("Internal SMTP servers must use port 25");
        });
    }
    
    public ValidateOptionsResult Validate(string name, EmailOptions options)
    {
        var result = Validate(options);
        return result.IsValid 
            ? ValidateOptionsResult.Success 
            : ValidateOptionsResult.Fail(result.Errors.Select(e => e.ErrorMessage));
    }
}

// Program.cs - Register validators
services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
services.Configure<EmailOptions>(Configuration.GetSection("email"))
    .ValidateOnStart();

// Validation with dependencies
public class ApiKeyValidator : IValidateOptions<ApiSettings>
{
    private readonly ISecretValidator _secretValidator;
    
    public ApiKeyValidator(ISecretValidator secretValidator)
    {
        _secretValidator = secretValidator;
    }
    
    public async Task<ValidateOptionsResult> ValidateAsync(string name, ApiSettings options)
    {
        // Validate against external service
        if (!await _secretValidator.IsValidApiKeyAsync(options.ApiKey))
        {
            return ValidateOptionsResult.Fail("Invalid API key");
        }
        
        return ValidateOptionsResult.Success;
    }
}
```

## Advanced Scenarios

### Custom Type Converters

```csharp
// TimeSpanConverter.cs - Support custom types in configuration
public class TimeSpanConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }
    
    public override object ConvertFrom(ITypeDescriptorContext context, 
        CultureInfo culture, object value)
    {
        // Support formats like "30s", "5m", "1h"
        if (value is string str)
        {
            if (str.EndsWith("s"))
                return TimeSpan.FromSeconds(int.Parse(str[..^1]));
            if (str.EndsWith("m"))
                return TimeSpan.FromMinutes(int.Parse(str[..^1]));
            if (str.EndsWith("h"))
                return TimeSpan.FromHours(int.Parse(str[..^1]));
                
            return TimeSpan.Parse(str);
        }
        
        return base.ConvertFrom(context, culture, value);
    }
}

// Usage in configuration class
[TypeConverter(typeof(TimeSpanConverter))]
public TimeSpan CacheExpiration { get; set; }
```

### Configuration Preprocessing

```csharp
// Program.cs - Transform configuration before use
builder.Configuration.AddYamlFile("config.yaml", optional: false)
    .Transform(config =>
    {
        // Replace environment variables
        var processed = Environment.ExpandEnvironmentVariables(config);
        
        // Custom placeholder resolution
        processed = ResolvePlaceholders(processed);
        
        // Decrypt sensitive values
        processed = DecryptSecureValues(processed);
        
        return processed;
    });
```

### Health Checks

```csharp
// YamlConfigurationHealthCheck.cs - Monitor configuration health
public class YamlConfigurationHealthCheck : IHealthCheck
{
    private readonly string _configPath;
    
    public YamlConfigurationHealthCheck(string configPath)
    {
        _configPath = configPath;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify file exists and is readable
            if (!File.Exists(_configPath))
                return Task.FromResult(HealthCheckResult.Unhealthy($"Configuration file not found: {_configPath}"));
                
            // Try to parse the file
            var yaml = File.ReadAllText(_configPath);
            var parser = new VYamlParser();
            parser.Parse(yaml);
            
            return Task.FromResult(HealthCheckResult.Healthy("Configuration file is valid"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Configuration error: {ex.Message}"));
        }
    }
}

// Program.cs - Register health check
services.AddHealthChecks()
    .AddTypeActivatedCheck<YamlConfigurationHealthCheck>(
        "config-yaml",
        args: new[] { "appsettings.yaml" });
```

## Troubleshooting

### Common Issues

**YAML parsing errors:**
```yaml
# INCORRECT - tabs not allowed for indentation
server:
	host: localhost  # This line uses a tab
```

```yaml
# CORRECT - use spaces (2 or 4 spaces per level)
server:
  host: localhost  # This line uses 2 spaces
```

**Type conversion failures:**
```csharp
// Program.cs - Enable detailed error messages
builder.Configuration.AddYamlFile("config.yaml", optional: false)
    .EnableDetailedErrors();

// This will now show the exact path and value that failed
// Example: "Failed to convert 'server:timeout' value 'thirty' to type 'System.Int32'"
```

**Validation errors:**
```csharp
// Clear validation error messages
try 
{
    var options = serviceProvider.GetRequiredService<IOptions<AppConfiguration>>();
    var config = options.Value; // Validation happens here
}
catch (OptionsValidationException ex)
{
    // Ex: "DataAnnotation validation failed for 'AppConfiguration' members: 
    //      'Database.ConnectionString' with the error: 'The ConnectionString field is required.'"
    Console.WriteLine($"Configuration validation failed: {ex.Message}");
    
    // Log all validation failures
    foreach (var failure in ex.Failures)
    {
        Console.WriteLine($"- {failure}");
    }
}
```

**Missing configuration values:**
```csharp
// Safe configuration access patterns

// Option 1: Use GetValue with defaults
var timeout = Configuration.GetValue<int>("server:timeout", 30);

// Option 2: Check if section exists
if (Configuration.GetSection("features:newFeature").Exists())
{
    // Feature configuration is present
    var feature = Configuration.GetSection("features:newFeature").Get<FeatureConfig>();
}

// Option 3: Use IOptionsSnapshot for optional configuration
public class OptionalService
{
    public OptionalService(IOptionsSnapshot<OptionalConfig> config)
    {
        // config.Value will have default values if section is missing
    }
}
```

## Migration Guide

### From JSON Configuration

```csharp
// Before - using JSON
builder.Configuration.AddJsonFile("appsettings.json");

// After - using YAML
builder.Configuration.AddYamlFile("appsettings.yaml");
```

Configuration structure remains identical—only the file format changes:

`// appsettings.json`
```json
{
  "server": {
    "host": "localhost",
    "port": 5000
  },
  "features": {
    "enabled": ["search", "analytics"]
  }
}
```

```yaml
# appsettings.yaml - Same structure, more readable
server:
  host: localhost
  port: 5000
  
features:
  enabled:
    - search
    - analytics
```

### Gradual Migration

You can use both JSON and YAML together during migration:

```csharp
// Program.cs - Use both providers during transition
builder.Configuration
    // Keep existing JSON files
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
    
    // New YAML files override JSON settings
    .AddYamlFile("appsettings.yaml", optional: true)
    .AddYamlFile($"appsettings.{env.EnvironmentName}.yaml", optional: true);

// Migrate sections incrementally:
// 1. Move 'database' section to YAML first
// 2. Test thoroughly  
// 3. Move next section
// 4. Eventually remove JSON files
```

## Requirements

- .NET 8.0, .NET 9.0, or .NET Standard 2.0
- No additional runtime dependencies

## Contributing

We welcome contributions! Please see our Contributing Guide for details.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/mistial-dev/VYaml.Configuration.git

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the sample application
dotnet run --project VYaml.Configuration.Sample

# Run benchmarks
sudo dotnet run -c Release --project benchmarks/VYaml.Configuration.Benchmarks
```

Refer to [docs/benchmarks.md](docs/benchmarks.md) for updating the performance summary document.

## Security

Found a security issue? Please email security@mistial.dev instead of using the issue tracker.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/mistial-dev/VYaml.Configuration/blob/master/LICENSE) file for details.

## Acknowledgments

Built on the excellent [VYaml](https://github.com/hadashiA/VYaml) parser by hadashiA. Special thanks to the .NET team for the extensible configuration framework.

## Support

- [Issue Tracker](https://github.com/mistial-dev/VYaml.Configuration/issues)

---

**Ready to simplify your configuration?** Install VYaml.Configuration and start using human-friendly YAML in your .NET applications today.
