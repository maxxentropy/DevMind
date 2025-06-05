using System.Text.Json.Serialization;

namespace DevMind.Shared.Models;

// ==================== JSON-RPC 2.0 BASE MODELS ====================

/// <summary>
/// Base JSON-RPC 2.0 request model
/// </summary>
public class McpJsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// Base JSON-RPC 2.0 response model
/// </summary>
public class McpJsonRpcResponse<T>
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }

    [JsonPropertyName("error")]
    public McpJsonRpcError? Error { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 error response model
/// </summary>
public class McpJsonRpcErrorResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("error")]
    public McpJsonRpcError? Error { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 error object
/// </summary>
public class McpJsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

// ==================== MCP INITIALIZATION MODELS ====================

/// <summary>
/// MCP initialize method parameters
/// </summary>
public class McpInitializeParams
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = string.Empty;

    [JsonPropertyName("capabilities")]
    public McpClientCapabilities Capabilities { get; set; } = new();

    [JsonPropertyName("clientInfo")]
    public McpClientInfo ClientInfo { get; set; } = new();
}

/// <summary>
/// MCP client capabilities
/// </summary>
public class McpClientCapabilities
{
    [JsonPropertyName("roots")]
    public McpRootsCapability? Roots { get; set; }

    [JsonPropertyName("sampling")]
    public object? Sampling { get; set; }
}

/// <summary>
/// MCP roots capability
/// </summary>
public class McpRootsCapability
{
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; }
}

/// <summary>
/// MCP client information
/// </summary>
public class McpClientInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// MCP initialize response
/// </summary>
public class McpInitializeResponse : McpJsonRpcResponse<McpInitializeResult>
{
}

/// <summary>
/// MCP initialize result
/// </summary>
public class McpInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = string.Empty;

    [JsonPropertyName("capabilities")]
    public McpServerCapabilities Capabilities { get; set; } = new();

    [JsonPropertyName("serverInfo")]
    public McpServerInfo ServerInfo { get; set; } = new();
}

/// <summary>
/// MCP server capabilities
/// </summary>
public class McpServerCapabilities
{
    [JsonPropertyName("tools")]
    public McpToolsCapability? Tools { get; set; }

    [JsonPropertyName("resources")]
    public McpResourcesCapability? Resources { get; set; }

    [JsonPropertyName("prompts")]
    public McpPromptsCapability? Prompts { get; set; }
}

/// <summary>
/// MCP tools capability
/// </summary>
public class McpToolsCapability
{
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; }
}

/// <summary>
/// MCP resources capability
/// </summary>
public class McpResourcesCapability
{
    [JsonPropertyName("subscribe")]
    public bool Subscribe { get; set; }

    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; }
}

/// <summary>
/// MCP prompts capability
/// </summary>
public class McpPromptsCapability
{
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; }
}

/// <summary>
/// MCP server information
/// </summary>
public class McpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

// ==================== MCP TOOLS MODELS ====================

/// <summary>
/// MCP tools/list response
/// </summary>
public class McpToolsListResponse : McpJsonRpcResponse<McpToolsListResult>
{
}

/// <summary>
/// MCP tools/list result
/// </summary>
public class McpToolsListResult
{
    [JsonPropertyName("tools")]
    public List<McpProtocolToolDefinition> Tools { get; set; } = new();
}

/// <summary>
/// MCP tool definition (JSON-RPC protocol format)
/// This represents the actual MCP protocol format with inputSchema
/// </summary>
public class McpProtocolToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("inputSchema")]
    public McpToolInputSchema InputSchema { get; set; } = new();
}

/// <summary>
/// MCP tool input schema (JSON Schema format)
/// </summary>
public class McpToolInputSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, McpSchemaProperty> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();

    [JsonPropertyName("additionalProperties")]
    public bool AdditionalProperties { get; set; } = false;
}

/// <summary>
/// MCP schema property definition
/// </summary>
public class McpSchemaProperty
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("default")]
    public object? Default { get; set; }

    [JsonPropertyName("enum")]
    public List<object>? Enum { get; set; }

    [JsonPropertyName("items")]
    public McpSchemaProperty? Items { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, McpSchemaProperty>? Properties { get; set; }
}

/// <summary>
/// MCP tools/call parameters
/// </summary>
public class McpToolCallParams
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public Dictionary<string, object> Arguments { get; set; } = new();
}

/// <summary>
/// MCP tools/call response
/// </summary>
public class McpToolCallResponse : McpJsonRpcResponse<McpToolResult>
{
}

/// <summary>
/// MCP tool execution result
/// </summary>
public class McpToolResult
{
    [JsonPropertyName("content")]
    public List<McpContent>? Content { get; set; }

    [JsonPropertyName("isError")]
    public bool? IsError { get; set; }
}

/// <summary>
/// MCP content object
/// </summary>
public class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
}

// ==================== MCP RESOURCES MODELS ====================

/// <summary>
/// MCP resources/list response
/// </summary>
public class McpResourcesListResponse : McpJsonRpcResponse<McpResourcesListResult>
{
}

/// <summary>
/// MCP resources/list result
/// </summary>
public class McpResourcesListResult
{
    [JsonPropertyName("resources")]
    public List<McpResource> Resources { get; set; } = new();
}

/// <summary>
/// MCP resource definition
/// </summary>
public class McpResource
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
}

/// <summary>
/// MCP resources/read parameters
/// </summary>
public class McpResourceReadParams
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

/// <summary>
/// MCP resources/read response
/// </summary>
public class McpResourceReadResponse : McpJsonRpcResponse<McpResourceReadResult>
{
}

/// <summary>
/// MCP resources/read result
/// </summary>
public class McpResourceReadResult
{
    [JsonPropertyName("contents")]
    public List<McpContent> Contents { get; set; } = new();
}

// ==================== MCP PROMPTS MODELS ====================

/// <summary>
/// MCP prompts/list response
/// </summary>
public class McpPromptsListResponse : McpJsonRpcResponse<McpPromptsListResult>
{
}

/// <summary>
/// MCP prompts/list result
/// </summary>
public class McpPromptsListResult
{
    [JsonPropertyName("prompts")]
    public List<McpPrompt> Prompts { get; set; } = new();
}

/// <summary>
/// MCP prompt definition
/// </summary>
public class McpPrompt
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("arguments")]
    public List<McpPromptArgument>? Arguments { get; set; }
}

/// <summary>
/// MCP prompt argument
/// </summary>
public class McpPromptArgument
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; }
}

/// <summary>
/// MCP prompts/get parameters
/// </summary>
public class McpPromptGetParams
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public Dictionary<string, string>? Arguments { get; set; }
}

/// <summary>
/// MCP prompts/get response
/// </summary>
public class McpPromptGetResponse : McpJsonRpcResponse<McpPromptGetResult>
{
}

/// <summary>
/// MCP prompts/get result
/// </summary>
public class McpPromptGetResult
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("messages")]
    public List<McpPromptMessage> Messages { get; set; } = new();
}

/// <summary>
/// MCP prompt message
/// </summary>
public class McpPromptMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public McpContent Content { get; set; } = new();
}

// ==================== LEGACY COMPATIBILITY MODELS ====================

/// <summary>
/// Legacy MCP tool request (for backward compatibility)
/// </summary>
public class McpToolRequest
{
    public string Tool { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? SessionId { get; set; }
}

/// <summary>
/// Legacy MCP tool response (for backward compatibility)
/// </summary>
public class McpToolResponse
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Legacy MCP tools response (for backward compatibility)
/// </summary>
public class McpToolsResponse
{
    public List<McpToolDefinition> Tools { get; set; } = new();
}
