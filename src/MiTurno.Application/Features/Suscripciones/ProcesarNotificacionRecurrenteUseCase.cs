using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Suscripciones;

/// <summary>
/// Procesa las notificaciones de webhook de la suscripción recurrente (Preapproval) de Mercado Pago:
/// "subscription_authorized_payment" es cada cargo periódico ya cobrado (renueva el vencimiento),
/// "subscription_preapproval" es un cambio de estado de la propia autorización (ej. el negocio la
/// canceló desde su cuenta de Mercado Pago en vez de hacerlo desde MiTurno). Igual que el resto de
/// los webhooks de pago del proyecto: nunca confía en el cuerpo/query recibido, siempre reconsulta
/// contra la API de Mercado Pago con el AccessToken de plataforma.
/// </summary>
public class ProcesarNotificacionRecurrenteUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway;
    private readonly IUnitOfWork _unitOfWork;

    public ProcesarNotificacionRecurrenteUseCase(
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

    public async Task<Result> ExecuteAsync(
        Guid suscripcionId, string tipo, string? externoId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externoId))
            return Result.Success();

        var suscripcion = await _suscripcionRepository.GetByIdAsync(suscripcionId, cancellationToken);
        if (suscripcion is null || suscripcion.MercadoPagoPreapprovalId is null)
            return Result.Success();

        if (string.Equals(tipo, "subscription_authorized_payment", StringComparison.OrdinalIgnoreCase))
            return await ProcesarCargoAsync(suscripcion, externoId, cancellationToken);

        if (string.Equals(tipo, "subscription_preapproval", StringComparison.OrdinalIgnoreCase))
            return await ProcesarCambioDeEstadoPreapprovalAsync(suscripcion, cancellationToken);

        return Result.Success();
    }

    private async Task<Result> ProcesarCargoAsync(Suscripcion suscripcion, string pagoExternoId, CancellationToken cancellationToken)
    {
        if (suscripcion.Pagos.Any(p => p.TransaccionExternalId == pagoExternoId))
            return Result.Success(); // ya procesado antes (idempotencia ante notificaciones duplicadas)

        var cargoResult = await _pagoRecurrenteGateway.ObtenerCargoRecurrenteAsync(
            _plataformaPagoConfiguracion.AccessToken, pagoExternoId, cancellationToken);
        if (cargoResult.IsFailure)
            return Result.Failure(cargoResult.Error!);

        var cargo = cargoResult.Value;
        if (cargo.PreapprovalId != suscripcion.MercadoPagoPreapprovalId)
            return Result.Success(); // notificación de otra suscripción, ignorar

        if (cargo.Estado != EstadoPagoExterno.Aprobado)
            return Result.Success(); // pendiente/rechazado: no renueva, se espera la próxima notificación

        try
        {
            var pago = PagoSuscripcion.Registrar(suscripcion.Id, cargo.Monto, pagoExternoId);
            pago.Aprobar();
            suscripcion.RegistrarPago(pago);

            var ahora = DateTime.UtcNow;
            var nuevoVencimiento = suscripcion.Plan.Periodicidad == Periodicidad.Mensual
                ? ahora.AddMonths(1)
                : ahora.AddYears(1);
            suscripcion.Renovar(nuevoVencimiento);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        _suscripcionRepository.Update(suscripcion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result> ProcesarCambioDeEstadoPreapprovalAsync(Suscripcion suscripcion, CancellationToken cancellationToken)
    {
        var estadoResult = await _pagoRecurrenteGateway.ObtenerPreapprovalAsync(
            _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId!, cancellationToken);
        if (estadoResult.IsFailure)
            return Result.Failure(estadoResult.Error!);

        if (estadoResult.Value.Status == "cancelled" && suscripcion.Estado != EstadoSuscripcion.Cancelada)
        {
            suscripcion.Cancelar();
            suscripcion.QuitarPreapproval();
            _suscripcionRepository.Update(suscripcion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
