using DevMind.CLI.Interfaces;

namespace DevMind.CLI.Services;

/// <summary>
/// Enhanced console service for user interaction and output formatting
/// Provides thread-safe console operations with color support and formatting options
/// </summary>
public class ConsoleService : IConsoleService
{
    #region Private Fields

    private readonly object _consoleLock = new();
    private readonly Dictionary<string, ConsoleColor> _logLevelColors;

    #endregion

    #region Constructor

    public ConsoleService()
    {
        _logLevelColors = new Dictionary<string, ConsoleColor>
        {
            ["ERROR"] = ConsoleColor.Red,
            ["WARN"] = ConsoleColor.Yellow,
            ["INFO"] = ConsoleColor.White,
            ["DEBUG"] = ConsoleColor.Gray,
            ["TRACE"] = ConsoleColor.DarkGray
        };
    }

    #endregion

    #region IConsoleService Implementation

    public async Task WriteAsync(string message, ConsoleColor? color = null)
    {
        await Task.Run(() =>
        {
            lock (_consoleLock)
            {
                if (color.HasValue)
                {
                    var originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = color.Value;
                    Console.Write(message);
                    Console.ForegroundColor = originalColor;
                }
                else
                {
                    Console.Write(message);
                }
            }
        });
    }

    public async Task WriteLineAsync(string? message = null, ConsoleColor? color = null)
    {
        await Task.Run(() =>
        {
            lock (_consoleLock)
            {
                if (color.HasValue)
                {
                    var originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = color.Value;
                    Console.WriteLine(message ?? string.Empty);
                    Console.ForegroundColor = originalColor;
                }
                else
                {
                    Console.WriteLine(message ?? string.Empty);
                }
            }
        });
    }

    public async Task WriteErrorAsync(string message)
    {
        await WriteLineAsync($"Error: {message}", ConsoleColor.Red);
    }

    public async Task WriteWarningAsync(string message)
    {
        await WriteLineAsync($"Warning: {message}", ConsoleColor.Yellow);
    }

    public async Task WriteSuccessAsync(string message)
    {
        await WriteLineAsync($"✅ {message}", ConsoleColor.Green);
    }

    public async Task WriteInfoAsync(string message)
    {
        await WriteLineAsync($"ℹ️  {message}", ConsoleColor.Cyan);
    }

    public async Task<string?> ReadLineAsync(string? prompt = null)
    {
        return await Task.Run(() =>
        {
            lock (_consoleLock)
            {
                if (!string.IsNullOrEmpty(prompt))
                {
                    Console.Write(prompt);
                }
                return Console.ReadLine();
            }
        });
    }

    public async Task<string> ReadLineRequiredAsync(string prompt, string? errorMessage = null)
    {
        string? input;
        do
        {
            input = await ReadLineAsync(prompt);
            if (string.IsNullOrWhiteSpace(input))
            {
                await WriteErrorAsync(errorMessage ?? "Input is required. Please try again.");
            }
        } while (string.IsNullOrWhiteSpace(input));

        return input;
    }

    public async Task<bool> PromptYesNoAsync(string question, bool defaultValue = false)
    {
        var defaultText = defaultValue ? "Y/n" : "y/N";
        var response = await ReadLineAsync($"{question} ({defaultText}): ");

        if (string.IsNullOrWhiteSpace(response))
        {
            return defaultValue;
        }

        var firstChar = response.Trim().ToLowerInvariant()[0];
        return firstChar == 'y';
    }

    public async Task<T?> PromptChoiceAsync<T>(string question, Dictionary<string, T> choices, T? defaultValue = default)
    {
        await WriteLineAsync(question);
        await WriteLineAsync();

        var choiceKeys = choices.Keys.ToList();
        for (int i = 0; i < choiceKeys.Count; i++)
        {
            var key = choiceKeys[i];
            var isDefault = defaultValue != null && choices[key]?.Equals(defaultValue) == true;
            var marker = isDefault ? " (default)" : "";
            await WriteLineAsync($"{i + 1}. {key}{marker}");
        }

        await WriteLineAsync();

        while (true)
        {
            var response = await ReadLineAsync("Enter choice number (or press Enter for default): ");

            if (string.IsNullOrWhiteSpace(response) && defaultValue != null)
            {
                return defaultValue;
            }

            if (int.TryParse(response, out var choice) && choice >= 1 && choice <= choiceKeys.Count)
            {
                var selectedKey = choiceKeys[choice - 1];
                return choices[selectedKey];
            }

            await WriteErrorAsync("Invalid choice. Please enter a valid number.");
        }
    }

    public async Task ClearAsync()
    {
        await Task.Run(() =>
        {
            lock (_consoleLock)
            {
                Console.Clear();
            }
        });
    }

    public async Task WriteTableAsync<T>(IEnumerable<T> items, params (string Header, Func<T, object?> ValueSelector)[] columns)
    {
        var itemList = items.ToList();
        if (!itemList.Any())
        {
            await WriteLineAsync("No items to display.", ConsoleColor.Gray);
            return;
        }

        // Calculate column widths
        var columnWidths = new int[columns.Length];
        for (int i = 0; i < columns.Length; i++)
        {
            var headerWidth = columns[i].Header.Length;
            var maxValueWidth = itemList.Max(item =>
            {
                var value = columns[i].ValueSelector(item);
                return value?.ToString()?.Length ?? 0;
            });
            columnWidths[i] = Math.Max(headerWidth, maxValueWidth) + 2; // Add padding
        }

        // Write header
        await WriteLineAsync();
        for (int i = 0; i < columns.Length; i++)
        {
            await WriteAsync(columns[i].Header.PadRight(columnWidths[i]), ConsoleColor.White);
        }
        await WriteLineAsync();

        // Write separator
        for (int i = 0; i < columns.Length; i++)
        {
            await WriteAsync(new string('-', columnWidths[i]), ConsoleColor.Gray);
        }
        await WriteLineAsync();

        // Write data rows
        foreach (var item in itemList)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                var value = columns[i].ValueSelector(item);
                var text = value?.ToString() ?? "";
                await WriteAsync(text.PadRight(columnWidths[i]));
            }
            await WriteLineAsync();
        }
        await WriteLineAsync();
    }

    public async Task WriteProgressAsync(string operation, int current, int total, ConsoleColor? color = null)
    {
        await Task.Run(() =>
        {
            lock (_consoleLock)
            {
                var percentage = total > 0 ? (current * 100) / total : 0;
                var progressBar = CreateProgressBar(percentage);
                var message = $"\r{operation}: {progressBar} {percentage}% ({current}/{total})";

                if (color.HasValue)
                {
                    var originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = color.Value;
                    Console.Write(message);
                    Console.ForegroundColor = originalColor;
                }
                else
                {
                    Console.Write(message);
                }
            }
        });
    }

    public async Task WriteJsonAsync<T>(T obj, bool indented = true)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        await WriteLineAsync(json, ConsoleColor.Cyan);
    }

    public async Task WriteBannerAsync(string title, ConsoleColor color = ConsoleColor.Cyan)
    {
        var width = Math.Max(title.Length + 4, 50);
        var border = new string('=', width);
        var paddedTitle = title.PadLeft((width + title.Length) / 2).PadRight(width);

        await WriteLineAsync(border, color);
        await WriteLineAsync(paddedTitle, color);
        await WriteLineAsync(border, color);
        await WriteLineAsync();
    }

    public async Task WriteKeyValueAsync(string key, object? value, int keyWidth = 20, ConsoleColor? keyColor = null, ConsoleColor? valueColor = null)
    {
        var paddedKey = $"{key}:".PadRight(keyWidth);
        await WriteAsync(paddedKey, keyColor ?? ConsoleColor.Yellow);
        await WriteLineAsync(value?.ToString() ?? "null", valueColor ?? ConsoleColor.White);
    }

    public async Task WriteBoxAsync(string content, ConsoleColor borderColor = ConsoleColor.Gray, ConsoleColor contentColor = ConsoleColor.White)
    {
        var lines = content.Split('\n', StringSplitOptions.None);
        var maxWidth = lines.Max(l => l.Length);
        var boxWidth = maxWidth + 4; // 2 chars padding on each side

        // Top border
        await WriteLineAsync($"┌{new string('─', boxWidth - 2)}┐", borderColor);

        // Content lines
        foreach (var line in lines)
        {
            await WriteAsync("│ ", borderColor);
            await WriteAsync(line.PadRight(maxWidth), contentColor);
            await WriteLineAsync(" │", borderColor);
        }

        // Bottom border
        await WriteLineAsync($"└{new string('─', boxWidth - 2)}┘", borderColor);
    }

    #endregion

    #region Private Helper Methods

    private static string CreateProgressBar(int percentage, int width = 20)
    {
        var filled = (int)((percentage / 100.0) * width);
        var empty = width - filled;
        return $"[{new string('█', filled)}{new string('░', empty)}]";
    }

    #endregion
}
