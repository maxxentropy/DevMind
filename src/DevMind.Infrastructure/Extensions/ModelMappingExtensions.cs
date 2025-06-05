using DevMind.Core.Domain.ValueObjects;
using DevMind.Shared.Models;

namespace DevMind.Infrastructure.Extensions;

/// <summary>
/// Extension methods for mapping between domain models and MCP protocol DTOs
/// Handles conversion between internal domain representations and MCP JSON-RPC protocol format
/// </summary>
public static class ModelMappingExtensions
{
    #region ToolDefinition Mapping

    /// <summary>
    /// Converts domain ToolDefinition to MCP protocol DTO
    /// </summary>
    /// <param name="domainTool">Domain tool definition</param>
    /// <returns>MCP protocol tool definition</returns>
    public static McpProtocolToolDefinition ToMcpProtocolDto(this ToolDefinition domainTool)
    {
        ArgumentNullException.ThrowIfNull(domainTool);

        return new McpProtocolToolDefinition
        {
            Name = domainTool.Name,
            Description = domainTool.Description,
            InputSchema = CreateInputSchemaFromParameters(domainTool.Parameters)
        };
    }

    /// <summary>
    /// Converts MCP protocol DTO to domain ToolDefinition
    /// </summary>
    /// <param name="mcpTool">MCP protocol tool definition</param>
    /// <returns>Domain tool definition</returns>
    public static ToolDefinition ToDomainModel(this McpProtocolToolDefinition mcpTool)
    {
        ArgumentNullException.ThrowIfNull(mcpTool);

        var parameters = CreateParametersFromInputSchema(mcpTool.InputSchema);
        var categories = ExtractCategoriesFromDescription(mcpTool.Description);

        return ToolDefinition.Create(
            mcpTool.Name,
            mcpTool.Description,
            parameters,
            categories,
            example: string.Empty); // MCP doesn't have explicit examples
    }

    /// <summary>
    /// Converts legacy MCP tool definition to domain model (for backward compatibility)
    /// </summary>
    /// <param name="legacyTool">Legacy MCP tool definition</param>
    /// <returns>Domain tool definition</returns>
    public static ToolDefinition ToDomainModel(this McpToolDefinition legacyTool)
    {
        ArgumentNullException.ThrowIfNull(legacyTool);

        var parameters = CreateParametersFromLegacyFormat(legacyTool.Parameters);
        var categories = legacyTool.Categories ?? new List<string>();

        return ToolDefinition.Create(
            legacyTool.Name,
            legacyTool.Description,
            parameters,
            categories,
            legacyTool.Example);
    }

    /// <summary>
    /// Converts domain ToolDefinition to legacy MCP format (for backward compatibility)
    /// </summary>
    /// <param name="domainTool">Domain tool definition</param>
    /// <returns>Legacy MCP tool definition</returns>
    public static McpToolDefinition ToLegacyMcpDto(this ToolDefinition domainTool)
    {
        ArgumentNullException.ThrowIfNull(domainTool);

        return new McpToolDefinition
        {
            Name = domainTool.Name,
            Description = domainTool.Description,
            Parameters = domainTool.Parameters.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToLegacyMcpParameter()),
            Categories = new List<string>(domainTool.Categories),
            Example = domainTool.Example
        };
    }

    /// <summary>
    /// Converts domain ToolParameter to legacy MCP parameter
    /// </summary>
    /// <param name="domainParameter">Domain tool parameter</param>
    /// <returns>Legacy MCP tool parameter</returns>
    public static McpToolParameter ToLegacyMcpParameter(this ToolParameter domainParameter)
    {
        ArgumentNullException.ThrowIfNull(domainParameter);

        return new McpToolParameter
        {
            Type = domainParameter.Type,
            Description = domainParameter.Description,
            Required = domainParameter.Required,
            DefaultValue = domainParameter.DefaultValue,
            AllowedValues = new List<object>(domainParameter.AllowedValues)
        };
    }

    /// <summary>
    /// Converts legacy MCP parameter to domain ToolParameter
    /// </summary>
    /// <param name="legacyParameter">Legacy MCP tool parameter</param>
    /// <returns>Domain tool parameter</returns>
    public static ToolParameter ToDomainParameter(this McpToolParameter legacyParameter)
    {
        ArgumentNullException.ThrowIfNull(legacyParameter);

        return ToolParameter.Create(
            legacyParameter.Type,
            legacyParameter.Description,
            legacyParameter.Required,
            legacyParameter.DefaultValue,
            legacyParameter.AllowedValues);
    }

    #endregion

    #region Collection Extensions

    /// <summary>
    /// Converts collection of domain tools to MCP protocol DTOs
    /// </summary>
    /// <param name="domainTools">Collection of domain tool definitions</param>
    /// <returns>Collection of MCP protocol tool definitions</returns>
    public static IEnumerable<McpProtocolToolDefinition> ToMcpProtocolDtos(this IEnumerable<ToolDefinition> domainTools)
    {
        ArgumentNullException.ThrowIfNull(domainTools);
        return domainTools.Select(tool => tool.ToMcpProtocolDto());
    }

    /// <summary>
    /// Converts collection of MCP protocol DTOs to domain tools
    /// </summary>
    /// <param name="mcpTools">Collection of MCP protocol tool definitions</param>
    /// <returns>Collection of domain tool definitions</returns>
    public static IEnumerable<ToolDefinition> ToDomainModels(this IEnumerable<McpProtocolToolDefinition> mcpTools)
    {
        ArgumentNullException.ThrowIfNull(mcpTools);
        return mcpTools.Select(tool => tool.ToDomainModel());
    }

    /// <summary>
    /// Converts collection of legacy MCP tools to domain tools
    /// </summary>
    /// <param name="legacyTools">Collection of legacy MCP tool definitions</param>
    /// <returns>Collection of domain tool definitions</returns>
    public static IEnumerable<ToolDefinition> ToDomainModels(this IEnumerable<McpToolDefinition> legacyTools)
    {
        ArgumentNullException.ThrowIfNull(legacyTools);
        return legacyTools.Select(tool => tool.ToDomainModel());
    }

    #endregion

    #region Legacy Compatibility

    /// <summary>
    /// Converts legacy MCP tool definition to protocol format
    /// </summary>
    /// <param name="legacyTool">Legacy MCP tool definition</param>
    /// <returns>MCP protocol tool definition</returns>
    public static McpProtocolToolDefinition ToProtocolFormat(this McpToolDefinition legacyTool)
    {
        ArgumentNullException.ThrowIfNull(legacyTool);

        // Convert legacy format to protocol format
        var parameters = CreateParametersFromLegacyFormat(legacyTool.Parameters);
        var inputSchema = CreateInputSchemaFromParameters(parameters);

        return new McpProtocolToolDefinition
        {
            Name = legacyTool.Name,
            Description = legacyTool.Description,
            InputSchema = inputSchema
        };
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Converts domain ToolParameter to MCP schema property
    /// </summary>
    /// <param name="domainParameter">Domain tool parameter</param>
    /// <returns>MCP schema property</returns>
    private static McpSchemaProperty ToMcpSchemaProperty(this ToolParameter domainParameter)
    {
        ArgumentNullException.ThrowIfNull(domainParameter);

        var property = new McpSchemaProperty
        {
            Type = ConvertTypeToJsonSchema(domainParameter.Type),
            Description = domainParameter.Description,
            Default = domainParameter.DefaultValue
        };

        if (domainParameter.AllowedValues?.Any() == true)
        {
            property.Enum = new List<object>(domainParameter.AllowedValues);
        }

        return property;
    }

    /// <summary>
    /// Converts MCP schema property to domain ToolParameter
    /// </summary>
    /// <param name="schemaProperty">MCP schema property</param>
    /// <param name="parameterName">Name of the parameter</param>
    /// <param name="isRequired">Whether the parameter is required</param>
    /// <returns>Domain tool parameter</returns>
    private static ToolParameter ToDomainParameter(this McpSchemaProperty schemaProperty, string parameterName, bool isRequired)
    {
        ArgumentNullException.ThrowIfNull(schemaProperty);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

        var domainType = ConvertJsonSchemaTypeToDomain(schemaProperty.Type);
        var allowedValues = schemaProperty.Enum ?? new List<object>();

        return ToolParameter.Create(
            domainType,
            schemaProperty.Description ?? $"Parameter {parameterName}",
            isRequired,
            schemaProperty.Default,
            allowedValues);
    }

    /// <summary>
    /// Creates domain parameters from legacy MCP parameter format
    /// </summary>
    /// <param name="legacyParameters">Legacy MCP parameters</param>
    /// <returns>Dictionary of domain parameters</returns>
    private static Dictionary<string, ToolParameter> CreateParametersFromLegacyFormat(Dictionary<string, McpToolParameter> legacyParameters)
    {
        var parameters = new Dictionary<string, ToolParameter>();

        if (legacyParameters == null)
            return parameters;

        foreach (var (name, legacyParam) in legacyParameters)
        {
            parameters[name] = legacyParam.ToDomainParameter();
        }

        return parameters;
    }

    /// <summary>
    /// Creates MCP input schema from domain parameters
    /// </summary>
    /// <param name="parameters">Domain parameters</param>
    /// <returns>MCP input schema</returns>
    private static McpToolInputSchema CreateInputSchemaFromParameters(Dictionary<string, ToolParameter> parameters)
    {
        var schema = new McpToolInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpSchemaProperty>(),
            Required = new List<string>(),
            AdditionalProperties = false
        };

        foreach (var (name, parameter) in parameters)
        {
            schema.Properties[name] = parameter.ToMcpSchemaProperty();

            if (parameter.Required)
            {
                schema.Required.Add(name);
            }
        }

        return schema;
    }

    /// <summary>
    /// Creates domain parameters from MCP input schema
    /// </summary>
    /// <param name="inputSchema">MCP input schema</param>
    /// <returns>Dictionary of domain parameters</returns>
    private static Dictionary<string, ToolParameter> CreateParametersFromInputSchema(McpToolInputSchema inputSchema)
    {
        var parameters = new Dictionary<string, ToolParameter>();

        if (inputSchema.Properties == null)
            return parameters;

        foreach (var (name, property) in inputSchema.Properties)
        {
            var isRequired = inputSchema.Required?.Contains(name) ?? false;
            parameters[name] = property.ToDomainParameter(name, isRequired);
        }

        return parameters;
    }

    /// <summary>
    /// Converts domain type to JSON Schema type
    /// </summary>
    /// <param name="domainType">Domain type string</param>
    /// <returns>JSON Schema type</returns>
    private static string ConvertTypeToJsonSchema(string domainType)
    {
        return domainType.ToLowerInvariant() switch
        {
            "string" => "string",
            "int" or "integer" => "integer",
            "long" => "integer",
            "float" or "double" or "decimal" => "number",
            "bool" or "boolean" => "boolean",
            "array" or "list" => "array",
            "object" or "dict" or "dictionary" => "object",
            _ => "string" // Default to string for unknown types
        };
    }

    /// <summary>
    /// Converts JSON Schema type to domain type
    /// </summary>
    /// <param name="jsonSchemaType">JSON Schema type</param>
    /// <returns>Domain type string</returns>
    private static string ConvertJsonSchemaTypeToDomain(string jsonSchemaType)
    {
        return jsonSchemaType.ToLowerInvariant() switch
        {
            "string" => "string",
            "integer" => "int",
            "number" => "double",
            "boolean" => "bool",
            "array" => "array",
            "object" => "object",
            _ => "string" // Default to string for unknown types
        };
    }

    /// <summary>
    /// Extracts categories from tool description (heuristic approach)
    /// </summary>
    /// <param name="description">Tool description</param>
    /// <returns>List of inferred categories</returns>
    private static List<string> ExtractCategoriesFromDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return new List<string>();

        var categories = new List<string>();
        var lowerDescription = description.ToLowerInvariant();

        // Common category keywords to look for
        var categoryKeywords = new Dictionary<string, string[]>
        {
            ["analysis"] = new[] { "analyze", "analysis", "examine", "inspect", "review" },
            ["generation"] = new[] { "generate", "create", "build", "make", "produce" },
            ["testing"] = new[] { "test", "testing", "validate", "verify", "check" },
            ["documentation"] = new[] { "document", "documentation", "doc", "readme" },
            ["workflow"] = new[] { "workflow", "process", "pipeline", "automation" },
            ["code"] = new[] { "code", "coding", "programming", "development" },
            ["file"] = new[] { "file", "files", "filesystem", "directory" },
            ["git"] = new[] { "git", "repository", "repo", "branch", "commit" },
            ["security"] = new[] { "security", "secure", "vulnerability", "scan" },
            ["performance"] = new[] { "performance", "optimize", "benchmark", "speed" }
        };

        foreach (var (category, keywords) in categoryKeywords)
        {
            if (keywords.Any(keyword => lowerDescription.Contains(keyword)))
            {
                categories.Add(category);
            }
        }

        return categories.Any() ? categories : new List<string> { "general" };
    }

    #endregion

    #region Tool Result Mapping

    /// <summary>
    /// Extracts result content from MCP tool result
    /// </summary>
    /// <param name="mcpResult">MCP tool execution result</param>
    /// <returns>Extracted result object</returns>
    public static object? ExtractResultContent(this McpToolResult mcpResult)
    {
        ArgumentNullException.ThrowIfNull(mcpResult);

        if (mcpResult.Content?.Any() != true)
            return null;

        var primaryContent = mcpResult.Content.First();

        // Return text content if available
        if (!string.IsNullOrWhiteSpace(primaryContent.Text))
            return primaryContent.Text;

        // Return data content if available
        if (primaryContent.Data != null)
            return primaryContent.Data;

        // Return content type information
        return new
        {
            Type = primaryContent.Type,
            MimeType = primaryContent.MimeType
        };
    }

    /// <summary>
    /// Creates MCP content from result object
    /// </summary>
    /// <param name="result">Result object</param>
    /// <param name="contentType">Content type</param>
    /// <returns>MCP content object</returns>
    public static McpContent CreateMcpContent(object? result, string contentType = "text")
    {
        var content = new McpContent
        {
            Type = contentType
        };

        if (result is string textResult)
        {
            content.Text = textResult;
            content.MimeType = "text/plain";
        }
        else if (result != null)
        {
            content.Data = result;
            content.MimeType = "application/json";
        }

        return content;
    }

    #endregion

    #region Validation Helpers

    /// <summary>
    /// Validates that an MCP protocol tool definition is well-formed
    /// </summary>
    /// <param name="mcpTool">MCP protocol tool definition to validate</param>
    /// <returns>Validation result with any errors</returns>
    public static ValidationResult ValidateMcpProtocolTool(this McpProtocolToolDefinition mcpTool)
    {
        ArgumentNullException.ThrowIfNull(mcpTool);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(mcpTool.Name))
            errors.Add("Tool name is required");

        if (string.IsNullOrWhiteSpace(mcpTool.Description))
            errors.Add("Tool description is required");

        if (mcpTool.InputSchema == null)
            errors.Add("Input schema is required");
        else
        {
            if (mcpTool.InputSchema.Type != "object")
                errors.Add("Input schema type must be 'object'");

            if (mcpTool.InputSchema.Properties == null)
                errors.Add("Input schema must have properties defined");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    /// <summary>
    /// Validates that a domain tool definition can be converted to MCP format
    /// </summary>
    /// <param name="domainTool">Domain tool definition to validate</param>
    /// <returns>Validation result with any errors</returns>
    public static ValidationResult ValidateForMcpConversion(this ToolDefinition domainTool)
    {
        ArgumentNullException.ThrowIfNull(domainTool);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(domainTool.Name))
            errors.Add("Tool name is required for MCP conversion");

        if (string.IsNullOrWhiteSpace(domainTool.Description))
            errors.Add("Tool description is required for MCP conversion");

        // Check for unsupported parameter types
        foreach (var (paramName, parameter) in domainTool.Parameters)
        {
            var jsonSchemaType = ConvertTypeToJsonSchema(parameter.Type);
            if (jsonSchemaType == "string" && parameter.Type.ToLowerInvariant() != "string")
            {
                errors.Add($"Parameter '{paramName}' has unsupported type '{parameter.Type}' that will be converted to string");
            }
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// Validation result for tool definition validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

#endregion
