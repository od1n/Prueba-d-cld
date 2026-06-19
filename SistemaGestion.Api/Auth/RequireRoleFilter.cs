using System.Security.Claims;

namespace SistemaGestion.Api.Auth;

/// <summary>
/// Endpoint filter que verifica el rol del usuario antes de ejecutar el handler.
/// Uso: .AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina))
/// </summary>
public static class RequireRole
{
    public static IEndpointFilter Of(Func<ClaimsPrincipal, bool> check)
        => new RoleFilter(check);

    private class RoleFilter(Func<ClaimsPrincipal, bool> _check) : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext ctx,
            EndpointFilterDelegate next)
        {
            var http = ctx.HttpContext;
            if (!_check(http.User))
                return Results.Forbid();
            return await next(ctx);
        }
    }
}
