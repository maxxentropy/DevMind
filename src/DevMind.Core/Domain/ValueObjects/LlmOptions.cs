namespace DevMind.Core.Domain.ValueObjects;

public class LlmOptions
{
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4000;
    public double Temperature { get; set; } = 0.1;
    public double TopP { get; set; } = 1.0;
    public int TopK { get; set; } = 40;
    public string[] StopSequences { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> AdditionalParameters { get; set; } = new();

    public static LlmOptions Default => new()
    {
        MaxTokens = 1000,
        Temperature = 0.1
    };

    public static LlmOptions ForAnalysis => new()
    {
        MaxTokens = 2000,
        Temperature = 0.1
    };

    public static LlmOptions ForSynthesis => new()
    {
        MaxTokens = 3000,
        Temperature = 0.3
    };
}
