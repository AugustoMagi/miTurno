using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Reservas;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Public;

/// <summary>
/// Procesa la notificación de webhook de Mercado Pago para el pago de una Reserva. Nunca confía en
/// el cuerpo/query recibido: siempre reconsulta el estado del pago directamente en la API de Mercado
/// Pago con el AccessToken real del negocio, y solo actúa si esa respuesta autenticada lo confirma.
/// Reusa ConfirmarPagoUseCase/RechazarPagoUseCase (misma lógica de dominio y emails que el flujo
/// manual) en vez de duplicarla. Devuelve Result.Success cuando no hay nada que hacer (para que el
/// controller responda 200 y Mercado Pago no reintente en vano) y Result.Failure solo ante errores
/// transitorios reales (para que Mercado Pago sí reintente la notificación más tarde).
/// </summary>
public class ProcesarNotificacionPagoMercadoPagoUseCase
{
    private readonly INegocioRepository _negocioRepository;
    private readonly IReservaRepository _reservaRepository;
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository;
    private readonly IPagoGateway _pagoGateway;
    private readonly ConfirmarPagoUseCase _confirmarPagoUseCase;
    private readonly RechazarPagoUseCase _rechazarPagoUseCase;

    public ProcesarNotificacionPagoMercadoPagoUseCase(
        INegocioRepository negocioRepository,
        IReservaRepository reservaRepository,
        IConfiguracionPagoRepository configuracionPagoRepository,
        IPagoGateway pagoGateway,
        ConfirmarPagoUseCase confirmarPagoUseCase,
        RechazarPagoUseCase rechazarPagoUseCase)
    {
        _negocioRepository = negocioRepository;
        _reservaRepository = reservaRepository;
        _configuracionPagoRepository = configuracionPagoRepository;
        _pagoGateway = pagoGateway;
        _confirmarPagoUseCase = confirmarPagoUseCase;
        _rechazarPagoUseCase = rechazarPagoUseCase;
    }

    public async Task<Result> ExecuteAsync(
        string slug, Guid reservaId, string? pagoExternoId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pagoExternoId))
            return Result.Success();

        var negocio = await _negocioRepository.GetBySlugAsync(slug, cancellationToken);
        if (negocio is null)
            return Result.Success();

        var configuracionPago = await _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocio.Id, cancellationToken);
        if (configuracionPago is not { Proveedor: ProveedorPago.MercadoPago, AccessToken: not null })
            return Result.Success();

        var reserva = await _reservaRepository.GetByIdAsync(reservaId, cancellationToken);
        if (reserva?.Pago is null || reserva.Pago.Estado != EstadoPago.Pendiente)
            return Result.Success(); // ya procesada antes (idempotencia ante notificaciones duplicadas) o no corresponde

        var estadoResult = await _pagoGateway.ObtenerEstadoPagoAsync(
            configuracionPago.AccessToken, pagoExternoId, cancellationToken);
        if (estadoResult.IsFailure)
            return Result.Failure(estadoResult.Error!);

        var estado = estadoResult.Value;
        if (estado.ExternalReference != reservaId.ToString())
            return Result.Success(); // notificación de otro pago, ignorar

        return estado.Estado switch
        {
            EstadoPagoExterno.Aprobado => AResult(await _confirmarPagoUseCase.ExecuteAsync(negocio.Id, reservaId, cancellationToken)),
            EstadoPagoExterno.Rechazado => AResult(await _rechazarPagoUseCase.ExecuteAsync(negocio.Id, reservaId, cancellationToken)),
            _ => Result.Success() // pending/in_process/etc.: esperar la próxima notificación
        };
    }

    private static Result AResult<T>(Result<T> result) =>
        result.IsSuccess ? Result.Success() : Result.Failure(result.Error!);
}
