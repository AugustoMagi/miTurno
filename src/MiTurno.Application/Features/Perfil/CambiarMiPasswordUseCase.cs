using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Perfil.Dtos;

namespace MiTurno.Application.Features.Perfil;

public class CambiarMiPasswordUseCase
{
    private readonly IValidator<CambiarMiPasswordRequest> _validator;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CambiarMiPasswordUseCase(
        IValidator<CambiarMiPasswordRequest> validator, IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(
        Guid usuarioId, CambiarMiPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
            return Result.Failure("Usuario no encontrado.");

        if (!_passwordHasher.Verify(request.PasswordActual, usuario.PasswordHash))
            return Result.Failure("La contraseña actual es incorrecta.");

        usuario.CambiarPassword(_passwordHasher.Hash(request.PasswordNueva));
        _usuarioRepository.Update(usuario);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
