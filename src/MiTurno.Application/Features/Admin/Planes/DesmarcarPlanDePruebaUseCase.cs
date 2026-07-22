using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Admin.Planes.Dtos;

namespace MiTurno.Application.Features.Admin.Planes;

/// <summary>
/// Quita la marca de "plan de prueba": los negocios nuevos dejan de arrancar con este plan hasta
/// que se marque otro. Deja al sistema sin plan de prueba si no se elige uno nuevo después.
/// </summary>
public class DesmarcarPlanDePruebaUseCase
{
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DesmarcarPlanDePruebaUseCase(IPlanRepository planRepository, IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlanResponse>> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
            return Result.Failure<PlanResponse>("Plan no encontrado.");

        plan.DesmarcarComoPlanDePrueba();
        _planRepository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(plan.ToResponse());
    }
}
