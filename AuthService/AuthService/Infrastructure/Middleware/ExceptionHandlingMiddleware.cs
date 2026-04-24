using Shared.Exceptions;
using ValidationException = Shared.Exceptions.ValidationException;

namespace AuthService.Infrastructure.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, exception.Message, exception.ErrorCode, exception.Errors);
        }
        catch (UnauthorizedException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, exception.Message, exception.ErrorCode);
        }
        catch (ForbiddenException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, exception.Message, exception.ErrorCode);
        }
        catch (NotFoundException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, exception.Message, exception.ErrorCode);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing request.");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "server_error");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message,
        string errorCode,
        IReadOnlyCollection<string>? errors = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            message,
            errorCode,
            errors = errors ?? Array.Empty<string>()
        });
    }
}
