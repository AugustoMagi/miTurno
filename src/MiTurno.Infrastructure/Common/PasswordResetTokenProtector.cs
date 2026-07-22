using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Infrastructure.Common;

/// <summary>
/// Cifra/descifra el token de "olvidé mi contraseña" con Data Protection (mismo mecanismo que
/// EstadoOAuthProtector), con vencimiento propio de 30 minutos.
/// </summary>
public class PasswordResetTokenProtector : IPasswordResetTokenProtector
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(30);

    private readonly ITimeLimitedDataProtector _protector;

    public PasswordResetTokenProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider
            .CreateProtector("MiTurno.Auth.PasswordReset")
            .ToTimeLimitedDataProtector();
    }

    public string Proteger(Guid usuarioId, string passwordHashActual)
    {
        var payload = $"{usuarioId}|{passwordHashActual}";
        return _protector.Protect(payload, Vigencia);
    }

    public Result<PasswordResetToken> Desproteger(string token)
    {
        try
        {
            var payload = _protector.Unprotect(token);
            var partes = payload.Split('|', 2);
            if (partes.Length != 2 || !Guid.TryParse(partes[0], out var usuarioId))
                return Result.Failure<PasswordResetToken>("El enlace no es válido.");

            return Result.Success(new PasswordResetToken(usuarioId, partes[1]));
        }
        catch (CryptographicException)
        {
            return Result.Failure<PasswordResetToken>("El enlace no es válido o expiró. Pedí uno nuevo.");
        }
    }
}
