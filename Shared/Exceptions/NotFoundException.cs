namespace Shared.Exceptions;

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message = "The requested resource was not found.") : base(message, "not_found")
    {
    }
}
