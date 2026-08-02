using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.Suscripciones;

/// <summary>
/// Reanuda un cobro automático que el propio negocio había pausado (ver CancelarMiSuscripcionUseCase):
/// reutiliza la misma Preapproval ya autorizada en Mercado Pago, así que no hace falta pasar de nuevo
/// por el checkout. Si no hay una Preapproval pausada (nunca se activó el cobro automático, o ya
/// está cancelada del todo), hay que ir por IniciarSuscripcionMercadoPagoUseCase en su lugar.
/// </summary>
public class ReanudarCobroAutomaticoUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway;
    private readonly IUnitOfWork _unitOfWork;

    public ReanudarCobroAutomaticoUseCase(
        ISuscripcionRepository suscripcionRepository,
        IPlataformaPagoConfiguracion plataformaPagoConfiguracion,
        IPagoRecurrenteGateway pagoRecurrenteGateway,
        IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _plataformaPagoConfiguracion = plataformaPagoConfiguracion;
        _pagoRecurrenteGateway = pagoRecurrenteGateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid negocioId, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure("Todavía no tenés una suscripción asignada.");

        if (suscripcion.MercadoPagoPreapprovalId is null || !suscripcion.CobroAutomaticoPausado)
            return Result.Failure("No tenés un cobro automático pausado para reanudar.");

        var reanudarResult = await _pagoRecurrenteGateway.ReanudarPreapprovalAsync(
            _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);
        if (reanudarResult.IsFailure)
            return Result.Failure(reanudarResult.Error!);

        suscripcion.ReanudarCobroAutomatico();
        _suscripcionRepository.Update(suscripcion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
