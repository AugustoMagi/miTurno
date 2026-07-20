using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Perfil.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Perfil;

public class ActualizarMiPerfilUseCase
{
    private readonly IValidator<ActualizarMiPerfilRequest> _validator;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarMiPerfilUseCase(
        IValidator<ActualizarMiPerfilRequest> validator, IUsuarioRepository usuarioRepository, IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MiPerfilResponse>> ExecuteAsync(
        Guid usuarioId, ActualizarMiPerfilRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<MiPerfilResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
            return Result.Failure<MiPerfilResponse>("Usuario no encontrado.");

        var usuarioConEseEmail = await _usuarioRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (usuarioConEseEmail is not null && usuarioConEseEmail.Id != usuarioId)
            return Result.Failure<MiPerfilResponse>("Ya existe un usuario con ese email.");

        try
        {
            usuario.ActualizarDatos(request.Nombre, request.Email);
            _usuarioRepository.Update(usuario);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(usuario.ToMiPerfilResponse());
        }
        catch (DomainException ex)
        {
            return Result.Failure<MiPerfilResponse>(ex.Message);
        }
    }
}
