using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Public.Dtos;

namespace MiTurno.Application.Features.Public;

/// <summary>
/// Consulta el estado actual de una reserva ya creada, identificada solo por el slug del negocio y
/// el id de la reserva (sin autenticación, igual que el resto del flujo público). La usa el cliente
/// al volver de Mercado Pago después de pagar: la confirmación real llega por webhook de forma
/// asíncrona, así que el frontend necesita poder repreguntar el estado en vez de asumirlo por la URL.
/// </summary>
public class ObtenerReservaClienteUseCase
{
    private readonly INegocioRepository _negocioRepository;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IReservaRepository _reservaRepository;

    public ObtenerReservaClienteUseCase(
        INegocioRepository negocioRepository,
        IRecursoRepository recursoRepository,
        IReservaRepository reservaRepository)
    {
        _negocioRepository = negocioRepository;
        _recursoRepository = recursoRepository;
        _reservaRepository = reservaRepository;
    }

    public async Task<Result<ReservaResponse>> ExecuteAsync(
        string slug, Guid reservaId, CancellationToken cancellationToken = default)
    {
        var negocio = await _negocioRepository.GetBySlugAsync(slug, cancellationToken);
        if (negocio is null || !negocio.Activo)
            return Result.Failure<ReservaResponse>("Reserva no encontrada.");

        var reserva = await _reservaRepository.GetByIdAsync(reservaId, cancellationToken);
        if (reserva is null)
            return Result.Failure<ReservaResponse>("Reserva no encontrada.");

        var recurso = await _recursoRepository.GetByIdAsync(reserva.RecursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocio.Id)
            return Result.Failure<ReservaResponse>("Reserva no encontrada.");

        return Result.Success(new ReservaResponse(
            reserva.Id, reserva.RecursoId, reserva.ClienteId, reserva.Fecha,
            reserva.HoraInicio, reserva.HoraFin, reserva.PrecioTotal, reserva.Estado));
    }
}
