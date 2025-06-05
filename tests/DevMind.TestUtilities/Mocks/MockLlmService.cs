// tests/DevMind.TestUtilities/Mocks/MockLlmService.cs

using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.TestUtilities.Mocks;

/// <summary>
/// Mock implementation of ILlmService for testing purposes
/// </summary>
public class MockLlmService : ILlmService
{
    #region Private Fields

    private readonly Queue<Result<UserIntent>> _intentResponses = new();
    private readonly Queue<Result<ExecutionPlan>> _planResponses = new();
    private readonly Queue<Result<string>> _synthesisResponses = new();
    private readonly Queue<Result<string>> _generationResponses = new();
    private readonly Queue<Result<bool>> _healthCheckResponses = new();

    #endregion

    #region Configuration Methods

    /// <summary>
    /// Configures the next intent analysis response
    /// </summary>
    /// <param name="response">The response to return</param>
    public void SetupIntentAnalysis(Result<UserIntent> response)
    {
        _intentResponses.Enqueue(response);
    }

    /// <summary>
    /// Configures the next execution plan response
    /// </summary>
    /// <param name="response">The response to return</param>
    public void SetupExecutionPlan(Result<ExecutionPlan> response)
    {
        _planResponses.Enqueue(response);
    }

    /// <summary>
    /// Configures the next synthesis response
    /// </summary>
    /// <param name="response">The response to return</param>
    public void SetupSynthesis(Result<string> response)
    {
        _synthesisResponses.Enqueue(response);
    }

    /// <summary>
    /// Configures the next generation response
    /// </summary>
    /// <param name="response">The response to return</param>
    public void SetupGeneration(Result<string> response)
    {
        _generationResponses.Enqueue(response);
    }

    /// <summary>
    /// Configures the next health check response
    /// </summary>
    /// <param name="response">The response to return</param>
    public void SetupHealthCheck(Result<bool> response)
    {
        _healthCheckResponses.Enqueue(response);
    }

    #endregion

    #region ILlmService Implementation

    public Task<Result<UserIntent>> AnalyzeIntentAsync(UserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_intentResponses.Count > 0)
        {
            return Task.FromResult(_intentResponses.Dequeue());
        }

        // Default response
        var intent = UserIntent.Create(request.Content, IntentType.AnalyzeCode);
        return Task.FromResult(Result<UserIntent>.Success(intent));
    }

    public Task<Result<ExecutionPlan>> CreateExecutionPlanAsync(UserIntent intent, IEnumerable<ToolDefinition> availableTools, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(availableTools);

        if (_planResponses.Count > 0)
        {
            return Task.FromResult(_planResponses.Dequeue());
        }

        // Default response
        var plan = ExecutionPlan.Create(intent);
        var toolCall = ToolCall.Create("mock_tool", new Dictionary<string, object> { ["input"] = intent.OriginalRequest });
        plan.AddStep(toolCall);

        return Task.FromResult(Result<ExecutionPlan>.Success(plan));
    }

    public Task<Result<string>> SynthesizeResponseAsync(UserIntent intent, ExecutionPlan plan, IEnumerable<ToolExecution> results, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(results);

        if (_synthesisResponses.Count > 0)
        {
            return Task.FromResult(_synthesisResponses.Dequeue());
        }

        // Default response
        var response = $"Mock synthesis for intent '{intent.OriginalRequest}' with {results.Count()} tool results.";
        return Task.FromResult(Result<string>.Success(response));
    }

    public Task<Result<string>> GenerateResponseAsync(string prompt, LlmOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        if (_generationResponses.Count > 0)
        {
            return Task.FromResult(_generationResponses.Dequeue());
        }

        // Default response
        var response = $"Mock generated response for prompt: {prompt.Substring(0, Math.Min(50, prompt.Length))}...";
        return Task.FromResult(Result<string>.Success(response));
    }

    public Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (_healthCheckResponses.Count > 0)
        {
            return Task.FromResult(_healthCheckResponses.Dequeue());
        }

        // Default response
        return Task.FromResult(Result<bool>.Success(true));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Clears all configured responses
    /// </summary>
    public void Reset()
    {
        _intentResponses.Clear();
        _planResponses.Clear();
        _synthesisResponses.Clear();
        _generationResponses.Clear();
        _healthCheckResponses.Clear();
    }

    /// <summary>
    /// Sets up a successful workflow with default responses
    /// </summary>
    public void SetupSuccessfulWorkflow()
    {
        Reset();

        var intent = UserIntent.Create("test request", IntentType.AnalyzeCode);
        SetupIntentAnalysis(Result<UserIntent>.Success(intent));

        var plan = ExecutionPlan.Create(intent);
        plan.AddStep(ToolCall.Create("test_tool"));
        SetupExecutionPlan(Result<ExecutionPlan>.Success(plan));

        SetupSynthesis(Result<string>.Success("Test synthesis response"));
        SetupGeneration(Result<string>.Success("Test generation response"));
        SetupHealthCheck(Result<bool>.Success(true));
    }

    /// <summary>
    /// Sets up a failing workflow with error responses
    /// </summary>
    /// <param name="errorCode">The error code to use</param>
    /// <param name="errorMessage">The error message to use</param>
    public void SetupFailingWorkflow(string errorCode = "TEST_ERROR", string errorMessage = "Test error")
    {
        Reset();

        SetupIntentAnalysis(Result<UserIntent>.Failure(errorCode, errorMessage));
        SetupExecutionPlan(Result<ExecutionPlan>.Failure(errorCode, errorMessage));
        SetupSynthesis(Result<string>.Failure(errorCode, errorMessage));
        SetupGeneration(Result<string>.Failure(errorCode, errorMessage));
        SetupHealthCheck(Result<bool>.Failure(errorCode, errorMessage));
    }

    #endregion
}
