# VYaml Configuration Examples

This directory contains comprehensive YAML configuration examples demonstrating various features and use cases supported by VYaml.Configuration.

## 📚 Example Files

### 1. **arrays-example.yaml**
Demonstrates various ways to work with arrays and collections:
- Simple string arrays
- Arrays of objects
- Nested arrays and matrices
- Mixed-type arrays
- Flow style (inline) arrays
- Empty array handling

### 2. **advanced-types.yaml**
Shows advanced YAML features and data types:
- Anchors and aliases for configuration reuse
- Multi-line strings (literal and folded styles)
- Special characters and escape sequences
- Various number formats (hex, octal, scientific notation)
- Boolean values in different formats
- Null values
- Dates and timestamps
- Binary data (base64)
- Explicit type tags
- Sets and ordered maps

### 3. **microservices-config.yaml**
Real-world example of configuring a microservices architecture:
- Service discovery configuration
- API gateway routing
- Database connections and pooling
- Message queue configuration
- Rate limiting rules
- Health checks and monitoring
- Security policies
- Auto-scaling configuration

### 4. **edge-cases.yaml**
Tests edge cases and special scenarios:
- Empty values and nulls
- Special characters in keys and values
- Reserved words as keys
- Very long values
- Deeply nested structures
- Case sensitivity
- Whitespace handling
- Comments in various positions
- Duplicate keys
- Security test cases (safe examples)

### 5. **environment-variables.yaml**
Patterns for environment variable substitution:
- Basic variable references with `${VAR}` syntax
- Default values with `${VAR:-default}`
- Nested variable references
- Complex configuration with multiple variables
- Lists from comma-separated environment variables
- Boolean and numeric values from environment

## 🔧 Usage Examples

### Loading a Specific Example

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddYamlFile("examples/arrays-example.yaml", optional: false)
    .Build();
```

### Binding to Strongly-Typed Classes

```csharp
// For the microservices example
public class ServicesConfiguration
{
    public Dictionary<string, ServiceConfig> Services { get; set; }
}

public class ServiceConfig
{
    public string Name { get; set; }
    public int Port { get; set; }
    public int Replicas { get; set; }
    public DatabaseConfig Database { get; set; }
}

// Bind the configuration
var servicesConfig = configuration.Get<ServicesConfiguration>();
```

### Handling Arrays

```csharp
// From arrays-example.yaml
var fruits = configuration.GetSection("Fruits").Get<List<string>>();
var users = configuration.GetSection("Users").Get<List<User>>();
```

### Working with Advanced Types

```csharp
// Multi-line strings
var documentation = configuration["Documentation"]; // Preserves line breaks
var description = configuration["Description"];    // Single line

// Numbers in different formats
var hex = configuration.GetValue<int>("Numbers:Hex");        // 255
var scientific = configuration.GetValue<double>("Numbers:Scientific"); // 6.022e23
```

## 🎯 Testing with Examples

You can use these examples to test various scenarios:

### 1. **Validation Testing**
```bash
dotnet run -- validate --file examples/edge-cases.yaml
```

### 2. **Performance Testing**
Use the large nested structures in `microservices-config.yaml` to test performance with complex configurations.

### 3. **Error Handling**
The `edge-cases.yaml` file contains scenarios that might cause parsing issues in some YAML parsers.

## 📝 Notes

### Environment Variable Substitution
VYaml.Configuration doesn't natively support `${VAR}` syntax. To implement this:

```csharp
public static class YamlConfigurationExtensions
{
    public static IConfigurationBuilder AddYamlFileWithEnvVars(
        this IConfigurationBuilder builder, 
        string path)
    {
        // Load YAML file
        var yaml = File.ReadAllText(path);
        
        // Replace environment variables
        yaml = Regex.Replace(yaml, @"\$\{(\w+)(?::-(.*?))?\}", match =>
        {
            var varName = match.Groups[1].Value;
            var defaultValue = match.Groups[2].Value;
            return Environment.GetEnvironmentVariable(varName) ?? defaultValue;
        });
        
        // Load processed YAML
        // ... implementation details
        
        return builder;
    }
}
```

### Special Characters
When using special characters in keys, quote them:
```yaml
"Key with spaces": value
"Key:with:colons": value
```

### Arrays in Configuration Keys
Arrays are flattened to configuration keys with indices:
```yaml
Fruits:
  - Apple
  - Banana
```
Becomes:
- `Fruits:0` = "Apple"
- `Fruits:1` = "Banana"

### Circular References
YAML anchors and aliases are resolved during parsing, so circular references are handled appropriately.

## 🚀 Try It Out

1. Copy any example file to your project
2. Load it with `AddYamlFile()`
3. Explore the configuration structure
4. Modify examples for your needs

These examples provide a solid foundation for understanding VYaml.Configuration capabilities!