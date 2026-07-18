using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Suscripciones;

/// <summary>
/// Procesa la notificación de webhook de Mercado Pago para el pago de una Suscripcion. Mismas
/// garantías que ProcesarNotificacionPagoMercadoPagoUseCase (reservas): nunca confía en el body
/// recibido, siempre reconsulta el estado real contra la API de MP (con el AccessToken de
/// plataforma, no el del negocio), y es idempotente ante notificaciones duplicadas.
/// </summary>
public class ProcesarNotificacionPagoSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoGateway _pagoGateway;
    private readonly IUnitOfWork _unitOfWork;

    public ProcesarNotificacionPagoSuscripcionUseCase(
        ISuscripcionRepository suscripcionRepository,
        IPlataformaPagoConfiguracion plataformaPagoConfiguracion,
        IPagoGateway pagoGateway,
        IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _plataformaPagoConfiguracion = plataformaPagoConfiguracion;
        _pagoGateway = pagoGateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(
        Guid suscripcionId, string? pagoExternoId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pagoExternoId))
            return Result.Success();

        var suscripcion = await _suscripcionRepository.GetByIdAsync(suscripcionId, cancellationToken);
        if (suscripcion is null)
            return Result.Success();

        var pagoPendiente = suscripcion.Pagos.FirstOrDefault(p => p.Estado == EstadoPago.Pendiente);
        if (pagoPendiente is null)
            return Result.Success(); // ya procesado antes (idempotencia) o no corresponde

        var estadoResult = await _pagoGateway.ObtenerEstadoPagoAsync(
            _plataformaPagoConfiguracion.AccessToken, pagoExternoId, cancellationToken);
        if (estadoResult.IsFailure)
            return Result.Failure(estadoResult.Error!);

        var estado = estadoResult.Value;
        if (estado.ExternalReference != pagoPendiente.Id.ToString())
            return Result.Success(); // notificación de otro pago, ignorar

        switch (estado.Estado)
        {
            case EstadoPagoExterno.Aprobado:
                pagoPendiente.Aprobar();
                var ahora = DateTime.UtcNow;
                var nuevoVencimiento = suscripcion.Plan.Periodicidad == Periodicidad.Mensual
                    ? ahora.AddMonths(1)
                    : ahora.AddYears(1);
                suscripcion.Renovar(nuevoVencimiento);
                break;
            case EstadoPagoExterno.Rechazado:
                pagoPendiente.Rechazar();
                break;
            default:
                return Result.Success(); // pending/in_process: esperar la próxima notificación
        }

        _suscripcionRepository.Update(suscripcion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
