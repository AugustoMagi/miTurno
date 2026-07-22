namespace MiTurno.Application.Common.Models;

/// <summary>Datos para enviarle al usuario el link de "olvidé mi contraseña".</summary>
public record NotificacionReseteoPassword(string UsuarioEmail, string UsuarioNombre, string LinkReseteo);
