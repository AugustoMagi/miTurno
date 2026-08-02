using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.Suscripciones;

/// <summary>
/// Si la suscripción tiene el cobro automático de Mercado Pago activado, lo pausa (no lo cancela
/// del todo): así, si el negocio se arrepiente, <see cref="ReanudarCobroAutomaticoUseCase"/> puede
/// reactivarlo con la misma Preapproval, sin que tenga que volver a autorizar desde el checkout de
/// Mercado Pago. Si pausar falla, no cancela localmente (para no dejar al negocio con el acceso
/// cortado mientras Mercado Pago le sigue cobrando igual). Si la Preapproval ya está cancelada del
/// todo en Mercado Pago (ej. el negocio la canceló desde su propia cuenta), no hay nada para pausar
/// ni para reanudar después: se suelta directamente. Cancelar acá sólo apaga la renovación
/// automática: <see cref="Domain.Entities.Suscripcion.EstaActiva"/> sigue dando acceso hasta la
/// fecha de vencimiento ya paga, no lo corta al toque.
/// </summary>
public class CancelarMiSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarMiSuscripcionUseCase(
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

        if (suscripcion.MercadoPagoPreapprovalId is not null)
        {
            var estadoResult = await _pagoRecurrenteGateway.ObtenerPreapprovalAsync(
                _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);

            if (estadoResult.IsSuccess && estadoResult.Value.Status == "cancelled")
            {
                suscripcion.QuitarPreapproval();
            }
            else
            {
                var yaPausadaEnMercadoPago = estadoResult.IsSuccess && estadoResult.Value.Status == "paused";
                if (!yaPausadaEnMercadoPago)
                {
                    var pausaResult = await _pagoRecurrenteGateway.PausarPreapprovalAsync(
                        _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);
                    if (pausaResult.IsFailure)
                        return Result.Failure(pausaResult.Error!);
                }

                suscripcion.PausarCobroAutomatico();
            }
        }

        suscripcion.Cancelar();
        _suscripcionRepository.Update(suscripcion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
