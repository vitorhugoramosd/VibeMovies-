using System.Net;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VibeMovies.Api.Infrastructure;

/// <summary>Bloqueia (403) qualquer requisição cuja origem não seja a própria máquina (loopback).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class LoopbackOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!IsLoopback(context.HttpContext.Connection.RemoteIpAddress))
        {
            context.Result = GlobalExceptionHandler.Problem(
                StatusCodes.Status403Forbidden,
                "A integração Jarvis só aceita requisições locais (localhost).",
                "Acesso negado");
        }
    }

    public static bool IsLoopback(IPAddress? address)
    {
        if (address is null) return false;
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        return IPAddress.IsLoopback(address);
    }
}
