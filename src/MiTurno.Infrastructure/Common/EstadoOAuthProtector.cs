using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Infrastructure.Common;

/// <summary>
/// Cifra/descifra el "state" del flujo OAuth de Mercado Pago con Data Protection (mismo mecanismo
/// que protege el AccessToken en la base, ver MiTurnoDbContext), con vencimiento propio: un intento
/// de conexión que no se completó en 15 minutos deja de ser válido.
/// </summary>
public class EstadoOAuthProtector : IEstadoOAuthProtector
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(15);

    private readonly ITimeLimitedDataProtector _protector;

    public EstadoOAuthProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider
            .CreateProtector("MiTurno.ConfiguracionPago.EstadoOAuth")
            .ToTimeLimitedDataProtector();
    }

    public string Proteger(Guid negocioId, string codeVerifier)
    {
        var payload = $"{negocioId}|{codeVerifier}";
        return _protector.Protect(payload, Vigencia);
    }

    public Result<EstadoOAuth> Desproteger(string state)
    {
        try
        {
            var payload = _protector.Unprotect(state);
            var partes = payload.Split('|', 2);
            if (partes.Length != 2 || !Guid.TryParse(partes[0], out var negocioId))
                return Result.Failure<EstadoOAuth>("El parámetro state no es válido.");

            return Result.Success(new EstadoOAuth(negocioId, partes[1]));
        }
        catch (CryptographicException)
        {
            return Result.Failure<EstadoOAuth>("El enlace de conexión con Mercado Pago no es válido o expiró. Probá de nuevo.");
        }
    }
}
