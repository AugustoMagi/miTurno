using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.Admin.Suscripciones;

/// <summary>
/// Igual criterio que CancelarMiSuscripcionUseCase: si hay cobro automático de Mercado Pago, se
/// cancela primero del lado de Mercado Pago — salvo que ya esté cancelado ahí (Mercado Pago
/// rechaza cancelar un Preapproval ya cancelado, y eso no debe bloquear la baja local).
/// </summary>
public class CancelarSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarSuscripcionUseCase(
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

    public async Task<Result> ExecuteAsync(Guid suscripcionId, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByIdAsync(suscripcionId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure("Suscripción no encontrada.");

        if (suscripcion.MercadoPagoPreapprovalId is not null)
        {
            var estadoResult = await _pagoRecurrenteGateway.ObtenerPreapprovalAsync(
                _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);
            var yaCanceladaEnMercadoPago = estadoResult.IsSuccess && estadoResult.Value.Status == "cancelled";

            if (!yaCanceladaEnMercadoPago)
            {
                var cancelacionResult = await _pagoRecurrenteGateway.CancelarPreapprovalAsync(
                    _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);
                if (cancelacionResult.IsFailure)
                    return Result.Failure(cancelacionResult.Error!);
            }

            suscripcion.QuitarPreapproval();
        }

        suscripcion.Cancelar();
        _suscripcionRepository.Update(suscripcion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
