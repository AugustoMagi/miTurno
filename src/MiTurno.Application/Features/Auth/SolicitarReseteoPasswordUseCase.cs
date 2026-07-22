using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Auth.Dtos;

namespace MiTurno.Application.Features.Auth;

/// <summary>
/// Pide el email de reseteo de contraseña. Siempre devuelve éxito, exista o no un Usuario con ese
/// email (y aunque esté inactivo): revelar si un email está registrado facilitaría enumerar cuentas.
/// El token va cifrado en el link, no se guarda nada en base.
/// </summary>
public class SolicitarReseteoPasswordUseCase
{
    private readonly IValidator<SolicitarReseteoPasswordRequest> _validator;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordResetTokenProtector _tokenProtector;
    private readonly IFrontendConfiguracion _frontendConfiguracion;
    private readonly IEmailNotificador _emailNotificador;

    public SolicitarReseteoPasswordUseCase(
        IValidator<SolicitarReseteoPasswordRequest> validator,
        IUsuarioRepository usuarioRepository,
        IPasswordResetTokenProtector tokenProtector,
        IFrontendConfiguracion frontendConfiguracion,
        IEmailNotificador emailNotificador)
    {
        _validator = validator;
        _usuarioRepository = usuarioRepository;
        _tokenProtector = tokenProtector;
        _frontendConfiguracion = frontendConfiguracion;
        _emailNotificador = emailNotificador;
    }

    public async Task<Result> ExecuteAsync(
        SolicitarReseteoPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var usuario = await _usuarioRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (usuario is null || !usuario.Activo)
            return Result.Success();

        var token = _tokenProtector.Proteger(usuario.Id, usuario.PasswordHash);
        var link = $"{_frontendConfiguracion.BaseUrl}/panel/restablecer-password?token={Uri.EscapeDataString(token)}";

        await _emailNotificador.NotificarReseteoPasswordAsync(
            new NotificacionReseteoPassword(usuario.Email, usuario.Nombre, link), cancellationToken);

        return Result.Success();
    }
}
