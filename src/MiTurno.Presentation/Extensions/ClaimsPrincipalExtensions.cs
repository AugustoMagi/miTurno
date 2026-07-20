using System.Security.Claims;

namespace MiTurno.Presentation.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetNegocioId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue("negocioId")!);

    public static Guid GetUsuarioId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue("usuarioId")!);
}
