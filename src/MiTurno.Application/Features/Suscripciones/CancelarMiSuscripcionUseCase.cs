using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.Suscripciones;

/// <summary>
/// Si la suscripción tiene el cobro automático de Mercado Pago activado, primero cancela la
/// Preapproval del lado de Mercado Pago: si eso falla, no cancela localmente (para no dejar al
/// negocio con el acceso cortado mientras Mercado Pago le sigue cobrando igual).
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
            var cancelacionResult = await _pagoRecurrenteGateway.CancelarPreapprovalAsync(
                _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);
            if (cancelacionResult.IsFailure)
                return Result.Failure(cancelacionResult.Error!);

            suscripcion.QuitarPreapproval();
        }

        suscripcion.Cancelar();
        _suscripcionRepository.Update(suscripcion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
