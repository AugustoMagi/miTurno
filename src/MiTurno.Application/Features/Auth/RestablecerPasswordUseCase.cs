using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Auth.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Auth;

/// <summary>
/// Aplica la contraseña nueva a partir de un token de reseteo. El token trae "de qué PasswordHash
/// partió" (ver PasswordResetTokenProtector): si no coincide con el actual, ya se usó antes o la
/// contraseña cambió por otra vía mientras tanto, y se rechaza en vez de dejarlo pasar.
/// </summary>
public class RestablecerPasswordUseCase
{
    private readonly IValidator<RestablecerPasswordRequest> _validator;
    private readonly IPasswordResetTokenProtector _tokenProtector;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RestablecerPasswordUseCase(
        IValidator<RestablecerPasswordRequest> validator,
        IPasswordResetTokenProtector tokenProtector,
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _tokenProtector = tokenProtector;
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(
        RestablecerPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var tokenResult = _tokenProtector.Desproteger(request.Token);
        if (tokenResult.IsFailure)
            return Result.Failure(tokenResult.Error!);
        var token = tokenResult.Value;

        var usuario = await _usuarioRepository.GetByIdAsync(token.UsuarioId, cancellationToken);
        if (usuario is null || !usuario.Activo || usuario.PasswordHash != token.PasswordHashEnEmision)
            return Result.Failure("El enlace no es válido o ya fue usado. Pedí uno nuevo.");

        try
        {
            usuario.CambiarPassword(_passwordHasher.Hash(request.PasswordNueva));
            _usuarioRepository.Update(usuario);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
