using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones.Dtos;

namespace MiTurno.Application.Features.Suscripciones;

public class ObtenerMiSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway;
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerMiSuscripcionUseCase(
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

    public async Task<Result<MiSuscripcionResponse>> ExecuteAsync(
        Guid negocioId, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure<MiSuscripcionResponse>("Todavía no tenés una suscripción asignada.");

        // Un intento de suscripción que el negocio nunca llegó a autorizar (o que canceló desde su
        // propia cuenta de Mercado Pago) puede quedar cancelado del lado de MP sin que nuestro
        // webhook se entere. Sin este chequeo, CobroAutomaticoActivo queda en true por error (solo
        // mira si hay un id guardado) y esconde el botón para volver a suscribirse.
        if (suscripcion.MercadoPagoPreapprovalId is not null)
        {
            var estadoResult = await _pagoRecurrenteGateway.ObtenerPreapprovalAsync(
                _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);

            if (estadoResult.IsSuccess && estadoResult.Value.Status == "cancelled")
            {
                suscripcion.QuitarPreapproval();
                _suscripcionRepository.Update(suscripcion);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return Result.Success(suscripcion.ToMiSuscripcionResponse());
    }
}
