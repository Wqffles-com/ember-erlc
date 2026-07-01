namespace Backend.Models;

public class ApiResponse<T>
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public string? Hint { get; set; }
    public T? Data { get; set; }

    public ApiResponse(int statusCode, string? hint, T? data)
    {
        StatusCode = statusCode;
        Message = GetMessageForStatusCode(statusCode);
        Hint = hint;
        Data = data;
    }

    private static string GetMessageForStatusCode(int code) => code switch
    {
        200 => "OK",
        201 => "Created",
        204 => "No Content",
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        500 => "Internal Server Error",
        _ => "Unknown"
    };

    public static ApiResponse<T> Success(T data) => new(200, null, data);
    public static ApiResponse<T> Created(T data) => new(201, null, data);
    public static ApiResponse<T> NoContent() => new(204, null, default);
    public static ApiResponse<T> Failure(int statusCode, string? hint = null) => new(statusCode, hint, default);
}

public static class ApiResponseHelper
{
    public static ApiResponse<object> Failure(int statusCode, string? hint = null) => new(statusCode, hint, null);
}
