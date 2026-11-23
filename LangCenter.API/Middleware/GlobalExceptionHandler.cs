using LangCenter.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Vi phạm quy tắc nghiệp vụ"),
            NotFoundException => (StatusCodes.Status404NotFound, "Không tìm thấy tài nguyên"),
            ConflictException => (StatusCodes.Status409Conflict, "Xung đột dữ liệu"),
            _ => (StatusCodes.Status500InternalServerError, "Lỗi hệ thống")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Request {Method} {Path} failed. TraceId: {TraceId}",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);
        else
            logger.LogWarning("Request {Method} {Path} returned {Status}. TraceId: {TraceId}",
                context.Request.Method, context.Request.Path, status, context.TraceIdentifier);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "Đã xảy ra lỗi. Vui lòng thử lại sau."
                : exception.Message,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken: ct);
        return true;
    }
}
