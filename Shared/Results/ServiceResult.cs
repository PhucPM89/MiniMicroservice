namespace Shared.Results;

public class ServiceResult
{
    protected ServiceResult(bool isSuccess, string message, string? errorCode = null, IReadOnlyCollection<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        ErrorCode = errorCode;
        Errors = errors ?? Array.Empty<string>();
    }

    public bool IsSuccess { get; }
    public string Message { get; }
    public string? ErrorCode { get; }
    public IReadOnlyCollection<string> Errors { get; }

    public static ServiceResult Success(string message = "Success")
    {
        return new ServiceResult(true, message);
    }

    public static ServiceResult Failure(string message, string? errorCode = null, IEnumerable<string>? errors = null)
    {
        return new ServiceResult(false, message, errorCode, errors?.ToArray());
    }
}
