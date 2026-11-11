using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace VibeMovies.Api.Infrastructure;

/// <summary>
/// Converte exceções não tratadas em ProblemDetails (RFC 7807) com o campo extra <c>message</c> em pt-BR,
/// que é o que o frontend lê (<c>errorBody.message</c>).
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, message) = exception switch
        {
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested
                => (499, "Requisição cancelada", "A requisição foi cancelada."),
            ArgumentException ae => (StatusCodes.Status400BadRequest, "Requisição inválida", ae.Message),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Requisição inválida", "Não foi possível ler a requisição."),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Acesso negado", "Acesso negado."),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno", "Ocorreu um erro inesperado. Tente novamente em instantes.")
        };

        if (status >= 500)
            _logger.LogError(exception, "Erro não tratado em {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = _env.IsDevelopment() && status >= 500 ? exception.Message : message,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["message"] = message;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", cancellationToken);
        return true;
    }

    /// <summary>ProblemDetails padronizado para respostas de erro montadas nos controllers.</summary>
    public static ObjectResult Problem(int status, string message, string? title = null)
    {
        var problem = new ProblemDetails { Status = status, Title = title ?? message, Detail = message };
        problem.Extensions["message"] = message;
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
