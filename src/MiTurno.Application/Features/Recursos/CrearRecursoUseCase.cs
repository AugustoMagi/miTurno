using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Recursos.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Recursos;

/// <summary>Da de alta un recurso (cancha) dentro del negocio del usuario autenticado.</summary>
public class CrearRecursoUseCase
{
    private readonly IValidator<CrearRecursoRequest> _validator;
    private readonly IRecursoRepository _recursoRepository;
    private readonly ValidarLimiteRecursosService _validarLimiteRecursosService;
    private readonly IUnitOfWork _unitOfWork;

    public CrearRecursoUseCase(
        IValidator<CrearRecursoRequest> validator,
        IRecursoRepository recursoRepository,
        ValidarLimiteRecursosService validarLimiteRecursosService,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _recursoRepository = recursoRepository;
        _validarLimiteRecursosService = validarLimiteRecursosService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RecursoResponse>> ExecuteAsync(
        Guid negocioId, CrearRecursoRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<RecursoResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var limiteResult = await _validarLimiteRecursosService.ValidarAsync(negocioId, cancellationToken);
        if (limiteResult.IsFailure)
            return Result.Failure<RecursoResponse>(limiteResult.Error!);

        try
        {
            var recurso = Recurso.Crear(
                negocioId, request.Nombre, request.Tipo,
                TimeSpan.FromMinutes(request.DuracionTurnoMinutos), request.Precio);

            await _recursoRepository.AddAsync(recurso, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(recurso.ToResponse());
        }
        catch (DomainException ex)
        {
            return Result.Failure<RecursoResponse>(ex.Message);
        }
    }
}
