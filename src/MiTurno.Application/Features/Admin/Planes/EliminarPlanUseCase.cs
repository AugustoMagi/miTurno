using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.Admin.Planes;

/// <summary>
/// Elimina un Plan de forma definitiva. A diferencia de Desactivar (baja lógica), esto borra la
/// fila: solo se permite si ningún negocio tuvo nunca una Suscripcion contra este plan, porque
/// Suscripcion.PlanId es Restrict y se perdería historial de facturación.
/// </summary>
public class EliminarPlanUseCase
{
    private readonly IPlanRepository _planRepository;
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EliminarPlanUseCase(
        IPlanRepository planRepository, ISuscripcionRepository suscripcionRepository, IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _suscripcionRepository = suscripcionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
            return Result.Failure("Plan no encontrado.");

        var tieneSuscripciones = await _suscripcionRepository.ExisteConPlanIdAsync(planId, cancellationToken);
        if (tieneSuscripciones)
            return Result.Failure(
                "No se puede eliminar: hay negocios con suscripciones (activas o históricas) contra este plan. Desactivalo en su lugar.");

        _planRepository.Remove(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
