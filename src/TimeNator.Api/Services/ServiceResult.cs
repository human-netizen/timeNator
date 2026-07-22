namespace TimeNator.Api.Services;

public enum ServiceError
{
    NotFound,
    Forbidden,
    Conflict,
    Invalid
}

/// <summary>
/// The outcome of an operation that can fail for a business reason. Controllers map
/// the error to a status code; services never know about HTTP.
/// </summary>
public record ServiceResult<T>(T? Value, ServiceError? Error = null, string? Message = null)
{
    public static ServiceResult<T> Ok(T value) => new(value);
    public static ServiceResult<T> Fail(ServiceError error, string message) => new(default, error, message);
}
