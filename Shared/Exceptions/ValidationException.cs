using System.Collections.ObjectModel;

namespace Shared.Exceptions;

public sealed class ValidationException : AppException
{

    public IReadOnlyCollection<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors, string message = "One or more validation errors occurred.") : base(message, "validation_error")
    {
        Errors = new ReadOnlyCollection<string>(errors.ToList());
    }
}
