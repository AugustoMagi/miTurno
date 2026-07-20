using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones.Dtos;

namespace MiTurno.Application.Features.Suscripciones;

public class CambiarPlanMiSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CambiarPlanMiSuscripcionUseCase(
        ISuscripcionRepository suscripcionRepository, IPlanRepository planRepository, IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MiSuscripcionResponse>> ExecuteAsync(
        Guid negocioId, CambiarPlanMiSuscripcionRequest request, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure<MiSuscripcionResponse>("Todavía no tenés una suscripción asignada.");

        var nuevoPlan = await _planRepository.GetByIdAsync(request.NuevoPlanId, cancellationToken);
        if (nuevoPlan is null || !nuevoPlan.Activo)
            return Result.Failure<MiSuscripcionResponse>("Plan no encontrado.");

        suscripcion.CambiarPlan(nuevoPlan);
        _suscripcionRepository.Update(suscripcion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(suscripcion.ToMiSuscripcionResponse());
    }
}
