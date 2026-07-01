using System.Text.Json;
using Backend.Models;

namespace Backend.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteErrorResponseAsync(context, 404, ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            await WriteErrorResponseAsync(context, 401, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteErrorResponseAsync(context, 409, ex.Message);
        }
        catch (Exception)
        {
            await WriteErrorResponseAsync(context, 500, "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string hint)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Failure(statusCode, hint);
        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
