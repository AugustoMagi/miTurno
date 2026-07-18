using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Admin.Planes.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Admin.Planes;

public class ActualizarPlanUseCase
{
    private readonly IValidator<ActualizarPlanRequest> _validator;
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarPlanUseCase(
        IValidator<ActualizarPlanRequest> validator, IPlanRepository planRepository, IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlanResponse>> ExecuteAsync(
        Guid planId, ActualizarPlanRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<PlanResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
            return Result.Failure<PlanResponse>("Plan no encontrado.");

        try
        {
            plan.Actualizar(
                request.Nombre, request.Precio, request.Periodicidad,
                request.LimiteRecursos, request.LimiteReservasPorMes);

            _planRepository.Update(plan);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(plan.ToResponse());
        }
        catch (DomainException ex)
        {
            return Result.Failure<PlanResponse>(ex.Message);
        }
    }
}
