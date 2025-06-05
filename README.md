# DevMind CLI Setup Instructions

## Quick Start

1. **Configure your LLM provider** (required for the system to work):

   Create `src/DevMind.CLI/appsettings.Development.json`:
   ```json
   {
     "Llm": {
       "Provider": "openai",
       "OpenAi": {
         "ApiKey": "your-openai-api-key-here"
       }
     }
   }
   ```

2. **Build and run**:
   ```bash
   dotnet build
   dotnet run --project src/DevMind.CLI test
   ```

## Available Commands

### Core Commands
- `devmind test` - Test the foundation components
- `devmind llm-test` - Test LLM connectivity and functionality
- `devmind status` - Check overall system status
- `devmind version` - Show version information

### Configuration Commands
- `devmind config validate` - Validate current configuration
- `devmind config show` - Show current configuration
- `devmind config show Llm` - Show specific section
- `devmind config providers` - List available LLM providers

### Processing Commands
- `devmind "your request here"` - Process a natural language request
- `devmind` - Interactive mode (prompts for input)

## Configuration

### LLM Providers

#### OpenAI (Recommended for testing)
```json
{
  "Llm": {
    "Provider": "openai",
    "OpenAi": {
      "ApiKey": "sk-your-key-here",
      "Model": "gpt-4-mini",
      "MaxTokens": 2048,
      "Temperature": 0.1
    }
  }
}
```

#### Anthropic Claude
```json
{
  "Llm": {
    "Provider": "anthropic",
    "Anthropic": {
      "ApiKey": "sk-ant-your-key-here",
      "Model": "claude-3-sonnet-20240229"
    }
  }
}
```

#### Ollama (Local)
```json
{
  "Llm": {
    "Provider": "ollama",
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "Model": "codellama"
    }
  }
}
```

### MCP Client

By default, the system uses a mock MCP client for testing. To use a real MCP server:

```json
{
  "McpClient": {
    "BaseUrl": "http://localhost:5000",
    "UseMockClient": false
  }
}
```

## Environment Variables

You can also configure using environment variables with the `DEVMIND_` prefix:

```bash
export DEVMIND_Llm__Provider=openai
export DEVMIND_Llm__OpenAi__ApiKey=your-key-here
```

## Testing the Setup

1. **Basic functionality**:
   ```bash
   dotnet run --project src/DevMind.CLI test
   ```

2. **LLM connectivity**:
   ```bash
   dotnet run --project src/DevMind.CLI llm-test
   ```

3. **Configuration validation**:
   ```bash
   dotnet run --project src/DevMind.CLI config validate
   ```

4. **System status**:
   ```bash
   dotnet run --project src/DevMind.CLI status
   ```

5. **Process a request**:
   ```bash
   dotnet run --project src/DevMind.CLI "analyze my code for bugs"
   ```

## Troubleshooting

### Common Issues

1. **"Cannot resolve scoped service" error**:
   - This has been fixed in the updated Program.cs
   - Make sure you're using the updated version

2. **LLM API key issues**:
   - Verify your API key is correctly configured
   - Run `devmind config show Llm` to check (API key will be masked)
   - Run `devmind llm-test` to test connectivity

3. **Missing configuration**:
   - Run `devmind config validate` to see what's missing
   - Copy the example configuration from appsettings.json

4. **MCP connection issues**:
   - The system defaults to mock MCP client
   - Check MCP configuration with `devmind config show McpClient`
   - Run `devmind status` to see MCP status

### Debug Mode

For detailed error information:
```bash
dotnet run --project src/DevMind.CLI --debug "your command"
```

## Architecture Overview

The system implements Clean Architecture with these layers:

- **DevMind.Core**: Domain models and interfaces
- **DevMind.Infrastructure**: LLM providers, MCP clients, external integrations
- **DevMind.CLI**: Command-line interface and user interactions
- **DevMind.Shared**: Shared models and utilities

### Key Components

1. **LLM Service**: Handles AI provider communication (OpenAI, Anthropic, Ollama, Azure)
2. **MCP Client**: Manages tool execution through Model Context Protocol
3. **Agent Orchestration**: Coordinates the full request processing pipeline
4. **Intent Analysis**: Determines what the user wants to accomplish
5. **Execution Planning**: Creates step-by-step tool execution plans
6. **Response Synthesis**: Converts raw results into user-friendly responses

## Development Workflow

### Making Changes

1. **Domain Logic**: Add new domain models in `DevMind.Core`
2. **LLM Integration**: Extend LLM providers in `DevMind.Infrastructure/LlmProviders`
3. **Tool Integration**: Add MCP clients in `DevMind.Infrastructure/McpClients`
4. **CLI Commands**: Add new commands in `DevMind.CLI/Commands`

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/DevMind.Core.Tests
dotnet test tests/DevMind.Infrastructure.Tests
dotnet test tests/DevMind.CLI.Tests
```

### Building

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/DevMind.CLI

# Build for release
dotnet build -c Release
```

## API Keys and Security

### Obtaining API Keys

#### OpenAI
1. Go to https://platform.openai.com
2. Sign up or log in
3. Navigate to API Keys
4. Create a new secret key
5. Copy the key (starts with `sk-`)

#### Anthropic
1. Go to https://console.anthropic.com
2. Sign up or log in
3. Navigate to API Keys
4. Create a new key
5. Copy the key (starts with `sk-ant-`)

### Security Best Practices

1. **Never commit API keys to version control**
2. **Use environment variables in production**
3. **Use appsettings.Development.json for local development**
4. **Set spending limits in your provider dashboards**
5. **Monitor usage and costs regularly**

### Example .env file (not tracked in git)
```bash
DEVMIND_Llm__OpenAi__ApiKey=sk-your-actual-key-here
DEVMIND_Llm__Anthropic__ApiKey=sk-ant-your-actual-key-here
```

## Performance Considerations

### LLM Provider Selection

- **OpenAI GPT-4 Mini**: Best balance of cost and performance for development
- **OpenAI GPT-4 Turbo**: Best for complex reasoning tasks
- **Anthropic Claude**: Good for analysis and coding tasks
- **Ollama**: Best for privacy and offline scenarios

### Optimization Tips

1. **Use appropriate token limits**: Don't request more tokens than needed
2. **Optimize temperature**: Lower values (0.1-0.3) for consistent results
3. **Enable caching**: Reduce redundant API calls
4. **Monitor costs**: Set spending alerts and limits

## Error Handling

The system uses the Result pattern for comprehensive error handling:

```csharp
var result = await llmService.GenerateResponseAsync(prompt);
if (result.IsSuccess)
{
    Console.WriteLine(result.Value);
}
else
{
    Console.WriteLine($"Error: {result.Error.Message}");
}
```

### Common Error Codes

- `LLM_AUTH_FAILED`: Invalid API key
- `LLM_RATE_LIMIT`: Too many requests
- `LLM_TIMEOUT`: Request took too long
- `TOOL_NOT_FOUND`: Requested tool unavailable
- `TOOL_EXECUTION_FAILED`: Tool execution error

## Contributing

### Code Style

- Follow Microsoft's C# coding conventions
- Use nullable reference types
- Apply SOLID principles
- Include XML documentation for public APIs
- Write unit tests for new functionality

### Pull Request Process

1. Create a feature branch
2. Implement changes with tests
3. Update documentation
4. Ensure all tests pass
5. Submit pull request with detailed description

## Support

For issues and questions:

1. Check this README and documentation
2. Run diagnostics: `devmind status` and `devmind config validate`
3. Enable debug logging for detailed error information
4. Check the logs in the console output

## License

[Add your license information here]
