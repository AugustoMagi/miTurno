using System.Security.Claims;

namespace MiTurno.Presentation.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetNegocioId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue("negocioId")!);
}
