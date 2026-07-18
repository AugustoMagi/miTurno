using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Admin.Planes.Dtos;

namespace MiTurno.Application.Features.Admin.Planes;

/// <summary>
/// Marca un Plan como el que reciben los negocios nuevos al registrarse (Suscripcion en prueba).
/// Solo puede haber un plan de prueba a la vez: si había otro marcado, se desmarca primero.
/// </summary>
public class MarcarPlanDePruebaUseCase
{
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarcarPlanDePruebaUseCase(IPlanRepository planRepository, IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlanResponse>> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
            return Result.Failure<PlanResponse>("Plan no encontrado.");

        var planDePruebaAnterior = await _planRepository.GetPlanDePruebaAsync(cancellationToken);
        if (planDePruebaAnterior is not null && planDePruebaAnterior.Id != plan.Id)
        {
            planDePruebaAnterior.DesmarcarComoPlanDePrueba();
            _planRepository.Update(planDePruebaAnterior);
        }

        plan.MarcarComoPlanDePrueba();
        _planRepository.Update(plan);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(plan.ToResponse());
    }
}
