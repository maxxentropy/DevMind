# DevMind - AI Development Agent

DevMind is an intelligent AI agent designed to assist with software development tasks. It integrates with MCP (Model Context Protocol) servers to provide sophisticated development assistance through natural language interactions.

## Features

- **Natural Language Processing**: Understand development requests in plain English
- **MCP Integration**: Seamlessly connects with DevFlow MCP server for tool execution
- **Clean Architecture**: Built with maintainable, testable architecture patterns
- **Multiple LLM Support**: Works with OpenAI, Anthropic Claude, and local Ollama models
- **CLI Interface**: Easy-to-use command-line interface for developer workflows

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- DevFlow MCP Server running on localhost:8080
- API key for your chosen LLM provider

### Installation

`ash
git clone <repository-url>
cd DevMind
dotnet restore
dotnet build
`

### Configuration

1. Copy ppsettings.json to ppsettings.Development.json
2. Configure your LLM provider settings:

`json
{
  "Llm": {
    "Provider": "openai",
    "OpenAi": {
      "ApiKey": "your-api-key-here"
    }
  }
}
`

### Usage

`ash
# Interactive mode
dotnet run --project src/DevMind.CLI

# Direct command
dotnet run --project src/DevMind.CLI -- "analyze my repository for potential improvements"

# Help
dotnet run --project src/DevMind.CLI -- --help
`

## Architecture

DevMind follows Clean Architecture principles with clear separation of concerns:

- **Core**: Domain entities and business logic
- **Infrastructure**: External integrations (MCP, LLM providers)
- **CLI**: Command-line interface and user interaction
- **Shared**: Common models and contracts

## Development

### Building

`ash
dotnet build
`

### Testing

`ash
dotnet test
`

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License

[License information]

## Support

For issues and questions, please use the GitHub issue tracker.
