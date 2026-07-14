using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Recursos.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Recursos;

/// <summary>Actualiza los datos de un recurso, verificando que pertenezca al negocio del usuario autenticado.</summary>
public class ActualizarRecursoUseCase
{
    private readonly IValidator<ActualizarRecursoRequest> _validator;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarRecursoUseCase(
        IValidator<ActualizarRecursoRequest> validator,
        IRecursoRepository recursoRepository,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _recursoRepository = recursoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RecursoResponse>> ExecuteAsync(
        Guid negocioId, Guid recursoId, ActualizarRecursoRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<RecursoResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var recurso = await _recursoRepository.GetByIdAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocioId)
            return Result.Failure<RecursoResponse>("Recurso no encontrado.");

        try
        {
            recurso.ActualizarDatos(
                request.Nombre, request.Tipo, TimeSpan.FromMinutes(request.DuracionTurnoMinutos), request.Precio);

            _recursoRepository.Update(recurso);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(recurso.ToResponse());
        }
        catch (DomainException ex)
        {
            return Result.Failure<RecursoResponse>(ex.Message);
        }
    }
}
