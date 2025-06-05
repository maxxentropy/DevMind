namespace DevMind.Core.Domain.ValueObjects;

public class ToolDefinition
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Dictionary<string, ToolParameter> Parameters { get; private set; } = new();
    public List<string> Categories { get; private set; } = new();
    public string Example { get; private set; } = string.Empty;

    private ToolDefinition() { } // EF Constructor

    public static ToolDefinition Create(string name, string description, 
        Dictionary<string, ToolParameter>? parameters = null, 
        List<string>? categories = null, 
        string? example = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new ToolDefinition
        {
            Name = name,
            Description = description,
            Parameters = parameters ?? new Dictionary<string, ToolParameter>(),
            Categories = categories ?? new List<string>(),
            Example = example ?? string.Empty
        };
    }

    public bool HasCategory(string category)
    {
        return Categories.Contains(category, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsRequired(string parameterName)
    {
        return Parameters.TryGetValue(parameterName, out var param) && param.Required;
    }
}

public class ToolParameter
{
    public string Type { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool Required { get; private set; }
    public object? DefaultValue { get; private set; }
    public List<object> AllowedValues { get; private set; } = new();

    private ToolParameter() { } // EF Constructor

    public static ToolParameter Create(string type, string description, bool required = false, 
        object? defaultValue = null, List<object>? allowedValues = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new ToolParameter
        {
            Type = type,
            Description = description,
            Required = required,
            DefaultValue = defaultValue,
            AllowedValues = allowedValues ?? new List<object>()
        };
    }
}
