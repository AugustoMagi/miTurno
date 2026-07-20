using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Public.Dtos;

public record ReservaResponse(
    Guid Id,
    Guid RecursoId,
    Guid ClienteId,
    DateOnly Fecha,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    decimal PrecioTotal,
    EstadoReserva Estado,
    string? LinkPago = null,
    string? AliasPago = null);
