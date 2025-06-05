namespace DevMind.CLI.Interfaces;

/// <summary>
/// Enhanced interface for console service operations
/// Provides comprehensive console interaction capabilities for CLI applications
/// </summary>
public interface IConsoleService
{
    #region Basic Output Operations

    /// <summary>
    /// Writes text to the console without a line terminator
    /// </summary>
    /// <param name="message">The text to write</param>
    /// <param name="color">Optional console color</param>
    Task WriteAsync(string message, ConsoleColor? color = null);

    /// <summary>
    /// Writes a line of text to the console
    /// </summary>
    /// <param name="message">The text to write (null writes empty line)</param>
    /// <param name="color">Optional console color</param>
    Task WriteLineAsync(string? message = null, ConsoleColor? color = null);

    #endregion

    #region Typed Output Operations

    /// <summary>
    /// Writes an error message with appropriate formatting
    /// </summary>
    /// <param name="message">The error message</param>
    Task WriteErrorAsync(string message);

    /// <summary>
    /// Writes a warning message with appropriate formatting
    /// </summary>
    /// <param name="message">The warning message</param>
    Task WriteWarningAsync(string message);

    /// <summary>
    /// Writes a success message with appropriate formatting
    /// </summary>
    /// <param name="message">The success message</param>
    Task WriteSuccessAsync(string message);

    /// <summary>
    /// Writes an informational message with appropriate formatting
    /// </summary>
    /// <param name="message">The informational message</param>
    Task WriteInfoAsync(string message);

    #endregion

    #region Input Operations

    /// <summary>
    /// Reads a line of input from the console
    /// </summary>
    /// <param name="prompt">Optional prompt to display</param>
    /// <returns>The input string, or null if EOF</returns>
    Task<string?> ReadLineAsync(string? prompt = null);

    /// <summary>
    /// Reads a required line of input, prompting until valid input is provided
    /// </summary>
    /// <param name="prompt">The prompt to display</param>
    /// <param name="errorMessage">Error message for empty input</param>
    /// <returns>The non-empty input string</returns>
    Task<string> ReadLineRequiredAsync(string prompt, string? errorMessage = null);

    /// <summary>
    /// Prompts for a yes/no response
    /// </summary>
    /// <param name="question">The question to ask</param>
    /// <param name="defaultValue">Default value if user presses Enter</param>
    /// <returns>True for yes, false for no</returns>
    Task<bool> PromptYesNoAsync(string question, bool defaultValue = false);

    /// <summary>
    /// Prompts user to choose from a list of options
    /// </summary>
    /// <typeparam name="T">Type of choice values</typeparam>
    /// <param name="question">The question to ask</param>
    /// <param name="choices">Dictionary of choice descriptions to values</param>
    /// <param name="defaultValue">Default value if user presses Enter</param>
    /// <returns>The selected choice value</returns>
    Task<T?> PromptChoiceAsync<T>(string question, Dictionary<string, T> choices, T? defaultValue = default);

    #endregion

    #region Advanced Formatting Operations

    /// <summary>
    /// Clears the console screen
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Writes data in a formatted table
    /// </summary>
    /// <typeparam name="T">Type of items to display</typeparam>
    /// <param name="items">Items to display in the table</param>
    /// <param name="columns">Column definitions</param>
    Task WriteTableAsync<T>(IEnumerable<T> items, params (string Header, Func<T, object?> ValueSelector)[] columns);

    /// <summary>
    /// Writes a progress indicator
    /// </summary>
    /// <param name="operation">Description of the operation</param>
    /// <param name="current">Current progress value</param>
    /// <param name="total">Total expected value</param>
    /// <param name="color">Optional color for the progress display</param>
    Task WriteProgressAsync(string operation, int current, int total, ConsoleColor? color = null);

    /// <summary>
    /// Writes an object as formatted JSON
    /// </summary>
    /// <typeparam name="T">Type of object to serialize</typeparam>
    /// <param name="obj">Object to write as JSON</param>
    /// <param name="indented">Whether to format with indentation</param>
    Task WriteJsonAsync<T>(T obj, bool indented = true);

    /// <summary>
    /// Writes a title banner with decorative formatting
    /// </summary>
    /// <param name="title">The title text</param>
    /// <param name="color">Color for the banner</param>
    Task WriteBannerAsync(string title, ConsoleColor color = ConsoleColor.Cyan);

    /// <summary>
    /// Writes a key-value pair with aligned formatting
    /// </summary>
    /// <param name="key">The key/label</param>
    /// <param name="value">The value</param>
    /// <param name="keyWidth">Width to pad the key to</param>
    /// <param name="keyColor">Color for the key</param>
    /// <param name="valueColor">Color for the value</param>
    Task WriteKeyValueAsync(string key, object? value, int keyWidth = 20, ConsoleColor? keyColor = null, ConsoleColor? valueColor = null);

    /// <summary>
    /// Writes content inside a decorative box
    /// </summary>
    /// <param name="content">Content to display in the box</param>
    /// <param name="borderColor">Color for the box border</param>
    /// <param name="contentColor">Color for the content text</param>
    Task WriteBoxAsync(string content, ConsoleColor borderColor = ConsoleColor.Gray, ConsoleColor contentColor = ConsoleColor.White);

    #endregion
}
