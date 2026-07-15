using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Recursos.Bloqueos.Dtos;

/// <summary>Reserva activa que queda en un recurso al bloquear una fecha en la que ya tenía turnos.</summary>
public record ReservaAfectadaResponse(
    Guid Id,
    Guid ClienteId,
    string ClienteNombre,
    string ClienteEmail,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    EstadoReserva Estado);
