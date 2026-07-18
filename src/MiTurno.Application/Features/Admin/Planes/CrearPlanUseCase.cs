using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Admin.Planes.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Admin.Planes;

/// <summary>Alta de un Plan del catálogo de precios de MiTurno (cobro de MiTurno a los negocios).</summary>
public class CrearPlanUseCase
{
    private readonly IValidator<CrearPlanRequest> _validator;
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CrearPlanUseCase(
        IValidator<CrearPlanRequest> validator, IPlanRepository planRepository, IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlanResponse>> ExecuteAsync(
        CrearPlanRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<PlanResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        try
        {
            var plan = Plan.Crear(
                request.Nombre, request.Precio, request.Periodicidad,
                request.LimiteRecursos, request.LimiteReservasPorMes);

            await _planRepository.AddAsync(plan, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(plan.ToResponse());
        }
        catch (DomainException ex)
        {
            return Result.Failure<PlanResponse>(ex.Message);
        }
    }
}
