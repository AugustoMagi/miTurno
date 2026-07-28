using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones.Dtos;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Suscripciones;

public class ObtenerMiSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway;
    private readonly ProcesarNotificacionRecurrenteUseCase _procesarNotificacionRecurrenteUseCase;
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerMiSuscripcionUseCase(
        ISuscripcionRepository suscripcionRepository,
        IPlataformaPagoConfiguracion plataformaPagoConfiguracion,
        IPagoRecurrenteGateway pagoRecurrenteGateway,
        ProcesarNotificacionRecurrenteUseCase procesarNotificacionRecurrenteUseCase,
        IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _plataformaPagoConfiguracion = plataformaPagoConfiguracion;
        _pagoRecurrenteGateway = pagoRecurrenteGateway;
        _procesarNotificacionRecurrenteUseCase = procesarNotificacionRecurrenteUseCase;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MiSuscripcionResponse>> ExecuteAsync(
        Guid negocioId, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure<MiSuscripcionResponse>("Todavía no tenés una suscripción asignada.");

        if (suscripcion.MercadoPagoPreapprovalId is not null)
        {
            var estadoResult = await _pagoRecurrenteGateway.ObtenerPreapprovalAsync(
                _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);

            // Un intento de suscripción que el negocio nunca llegó a autorizar (o que canceló desde
            // su propia cuenta de Mercado Pago) puede quedar cancelado del lado de MP sin que nuestro
            // webhook se entere. Sin este chequeo, CobroAutomaticoActivo queda en true por error (solo
            // mira si hay un id guardado) y esconde el botón para volver a suscribirse.
            if (estadoResult.IsSuccess && estadoResult.Value.Status == "cancelled")
            {
                suscripcion.QuitarPreapproval();
                _suscripcionRepository.Update(suscripcion);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            // Mercado Pago ya autorizó (y probablemente cobró) pero acá seguimos sin activar: el
            // webhook puede no habernos llegado nunca (ej. la app estaba "dormida" en ese momento).
            // En vez de depender pura y exclusivamente de esa notificación, reconciliamos acá mismo
            // reprocesando el último cargo como si el webhook recién hubiera llegado — mismo código,
            // mismas garantías de idempotencia.
            else if (estadoResult.IsSuccess && estadoResult.Value.Status == "authorized"
                && suscripcion.Estado != EstadoSuscripcion.Activa)
            {
                var ultimoCargoResult = await _pagoRecurrenteGateway.BuscarUltimoCargoIdAsync(
                    _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);

                if (ultimoCargoResult.IsSuccess && ultimoCargoResult.Value is not null)
                {
                    await _procesarNotificacionRecurrenteUseCase.ExecuteAsync(
                        suscripcion.Id, "subscription_authorized_payment", ultimoCargoResult.Value, cancellationToken);

                    suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken) ?? suscripcion;
                }
            }
        }

        return Result.Success(suscripcion.ToMiSuscripcionResponse());
    }
}
