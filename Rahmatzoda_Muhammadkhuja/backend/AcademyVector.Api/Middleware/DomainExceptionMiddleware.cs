using AcademyVector.Api.Domain.Exceptions;

namespace AcademyVector.Api.Middleware;

public sealed class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public DomainExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ConflictDomainException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            object? conflictingWebinar = null;
            if (ex.Details != null)
            {
                var prop = ex.Details.GetType().GetProperty("conflictingWebinar");
                conflictingWebinar = prop?.GetValue(ex.Details);
            }

            await context.Response.WriteAsJsonAsync(new
            {
                error = ex.Message,
                code = ex.ErrorCode,
                conflictingWebinar
            });
        }
        catch (DomainException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(new
            {
                error = ex.Message,
                code = ex.ErrorCode
            });
        }
    }
}
