using DevMind.Core.Domain.ValueObjects;
using DevMind.Shared.Models;

namespace DevMind.Infrastructure.Extensions;

/// <summary>
/// Extension methods for mapping between domain models and external DTOs
/// </summary>
public static class ModelMappingExtensions
{
    #region ToolDefinition Mapping

    /// <summary>
    /// Converts domain ToolDefinition to MCP DTO
    /// </summary>
    public static McpToolDefinition ToMcpDto(this ToolDefinition domainTool)
    {
        ArgumentNullException.ThrowIfNull(domainTool);

        return new McpToolDefinition
        {
            Name = domainTool.Name,
            Description = domainTool.Description,
            Parameters = domainTool.Parameters.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToMcpDto()),
            Categories = new List<string>(domainTool.Categories),
            Example = domainTool.Example
        };
    }

    /// <summary>
    /// Converts MCP DTO to domain ToolDefinition
    /// </summary>
    public static ToolDefinition ToDomainModel(this McpToolDefinition mcpTool)
    {
        ArgumentNullException.ThrowIfNull(mcpTool);

        var parameters = mcpTool.Parameters?.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToDomainModel()) ?? new Dictionary<string, ToolParameter>();

        return ToolDefinition.Create(
            mcpTool.Name,
            mcpTool.Description,
            parameters,
            mcpTool.Categories,
            mcpTool.Example);
    }

    /// <summary>
    /// Converts domain ToolParameter to MCP DTO
    /// </summary>
    public static McpToolParameter ToMcpDto(this ToolParameter domainParameter)
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
    /// Converts MCP DTO to domain ToolParameter
    /// </summary>
    public static ToolParameter ToDomainModel(this McpToolParameter mcpParameter)
    {
        ArgumentNullException.ThrowIfNull(mcpParameter);

        return ToolParameter.Create(
            mcpParameter.Type,
            mcpParameter.Description,
            mcpParameter.Required,
            mcpParameter.DefaultValue,
            mcpParameter.AllowedValues);
    }

    #endregion

    #region Collection Extensions

    /// <summary>
    /// Converts collection of domain tools to MCP DTOs
    /// </summary>
    public static IEnumerable<McpToolDefinition> ToMcpDtos(this IEnumerable<ToolDefinition> domainTools)
    {
        ArgumentNullException.ThrowIfNull(domainTools);
        return domainTools.Select(tool => tool.ToMcpDto());
    }

    /// <summary>
    /// Converts collection of MCP DTOs to domain tools
    /// </summary>
    public static IEnumerable<ToolDefinition> ToDomainModels(this IEnumerable<McpToolDefinition> mcpTools)
    {
        ArgumentNullException.ThrowIfNull(mcpTools);
        return mcpTools.Select(tool => tool.ToDomainModel());
    }

    #endregion
}
