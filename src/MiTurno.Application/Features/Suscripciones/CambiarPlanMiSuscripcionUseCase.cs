using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones.Dtos;

namespace MiTurno.Application.Features.Suscripciones;

public class CambiarPlanMiSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway;
    private readonly IUnitOfWork _unitOfWork;

    public CambiarPlanMiSuscripcionUseCase(
        ISuscripcionRepository suscripcionRepository,
        IPlanRepository planRepository,
        IPlataformaPagoConfiguracion plataformaPagoConfiguracion,
        IPagoRecurrenteGateway pagoRecurrenteGateway,
        IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _planRepository = planRepository;
        _plataformaPagoConfiguracion = plataformaPagoConfiguracion;
        _pagoRecurrenteGateway = pagoRecurrenteGateway;
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

        // Best-effort: si falla la actualización en Mercado Pago (caída puntual), el cambio de plan
        // local ya quedó guardado igual; el monto se termina de sincronizar en la próxima renovación.
        if (suscripcion.MercadoPagoPreapprovalId is not null)
        {
            await _pagoRecurrenteGateway.ActualizarMontoPreapprovalAsync(
                _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId,
                nuevoPlan.Precio, cancellationToken);
        }

        return Result.Success(suscripcion.ToMiSuscripcionResponse());
    }
}
