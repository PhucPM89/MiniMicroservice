namespace Shared.Results;

public sealed class ServiceResult<T> : ServiceResult
{
    private ServiceResult(bool isSuccess, T? data, string message, string? errorCode = null, IReadOnlyCollection<string>? errors = null)
        : base(isSuccess, message, errorCode, errors)
    {
        Data = data;
    }

    public T? Data { get; }

    public static ServiceResult<T> Success(T data, string message = "Success")
    {
        return new ServiceResult<T>(true, data, message);
    }

    public new static ServiceResult<T> Failure(string message, string? errorCode = null, IEnumerable<string>? errors = null)
    {
        return new ServiceResult<T>(false, default, message, errorCode, errors?.ToArray());
    }
}
