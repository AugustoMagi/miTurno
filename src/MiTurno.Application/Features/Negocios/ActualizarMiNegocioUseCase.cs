using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Negocios.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Negocios;

public class ActualizarMiNegocioUseCase
{
    private readonly IValidator<ActualizarMiNegocioRequest> _validator;
    private readonly INegocioRepository _negocioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarMiNegocioUseCase(
        IValidator<ActualizarMiNegocioRequest> validator, INegocioRepository negocioRepository, IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _negocioRepository = negocioRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MiNegocioResponse>> ExecuteAsync(
        Guid negocioId, ActualizarMiNegocioRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<MiNegocioResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var negocio = await _negocioRepository.GetByIdAsync(negocioId, cancellationToken);
        if (negocio is null)
            return Result.Failure<MiNegocioResponse>("Negocio no encontrado.");

        try
        {
            negocio.ActualizarDatos(request.Nombre, request.Descripcion, request.Direccion, request.Telefono);
            _negocioRepository.Update(negocio);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(negocio.ToMiNegocioResponse());
        }
        catch (DomainException ex)
        {
            return Result.Failure<MiNegocioResponse>(ex.Message);
        }
    }
}
