using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Suscripciones;

/// <summary>
/// Permite que un negocio sin Suscripcion asignada (ej. registrado antes de que existiera algún
/// Plan) elija uno por primera vez. Distinto de CambiarPlanMiSuscripcionUseCase, que requiere que
/// ya exista una Suscripcion.
/// </summary>
public class ElegirPlanUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ElegirPlanUseCase(
        ISuscripcionRepository suscripcionRepository,
        IPlanRepository planRepository,
        IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MiSuscripcionResponse>> ExecuteAsync(
        Guid negocioId, ElegirPlanRequest request, CancellationToken cancellationToken = default)
    {
        var existente = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (existente is not null)
            return Result.Failure<MiSuscripcionResponse>("Ya tenés una suscripción asignada.");

        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null || !plan.Activo)
            return Result.Failure<MiSuscripcionResponse>("Plan no encontrado.");

        var suscripcion = Suscripcion.Elegir(negocioId, plan);
        await _suscripcionRepository.AddAsync(suscripcion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(suscripcion.ToMiSuscripcionResponse());
    }
}
