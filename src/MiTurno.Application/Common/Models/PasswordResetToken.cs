namespace MiTurno.Application.Common.Models;

/// <summary>
/// Contenido de un token de reseteo de contraseña ya descifrado: a qué usuario pertenece y qué
/// PasswordHash tenía en el momento en que se emitió, para poder invalidarlo si ya se usó o si la
/// contraseña cambió por otra vía mientras tanto.
/// </summary>
public record PasswordResetToken(Guid UsuarioId, string PasswordHashEnEmision);
