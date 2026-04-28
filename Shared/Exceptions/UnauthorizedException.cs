namespace Shared.Exceptions;

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Authentication is required to access this resource.") : base(message, "unauthorized")
    {
    }
}
