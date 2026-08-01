namespace MiTurno.Application.Features.Recursos.Bloqueos.Dtos;

public record BloqueoFechaResponse(
    Guid Id,
    Guid RecursoId,
    DateOnly Fecha,
    TimeSpan? HoraInicio,
    TimeSpan? HoraFin,
    string? Motivo,
    IReadOnlyList<ReservaAfectadaResponse> ReservasAfectadas);
