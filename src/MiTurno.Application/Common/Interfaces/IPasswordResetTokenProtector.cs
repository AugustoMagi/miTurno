using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Common.Interfaces;

/// <summary>
/// Empaqueta un token de reseteo de contraseña cifrado y con vencimiento propio (mismo mecanismo
/// que IEstadoOAuthProtector), sin necesitar guardar nada en base entre pedirlo y usarlo.
/// </summary>
public interface IPasswordResetTokenProtector
{
    string Proteger(Guid usuarioId, string passwordHashActual);

    Result<PasswordResetToken> Desproteger(string token);
}
