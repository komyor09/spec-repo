using System.Text.Encodings.Web;
using System.Text.Json;
using VetMed.Api.Domain.Exceptions;

namespace VetMed.Api.Middleware;

public sealed class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly JsonSerializerOptions ErrorJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

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
        catch (ClinicDomainException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            var payload = JsonSerializer.Serialize(new { error = ex.Message }, ErrorJsonOptions);
            await context.Response.WriteAsync(payload);
        }
    }
}
