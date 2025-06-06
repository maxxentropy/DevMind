using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevMind.CLI.Interfaces;

public interface ICliErrorHandler
{
    Task<int> HandleErrorAsync(Exception exception, string? context = null);
    Task<int> HandleValidationErrorsAsync(IEnumerable<string> errors, string? context = null);
}
