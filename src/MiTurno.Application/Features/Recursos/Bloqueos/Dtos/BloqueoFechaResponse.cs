namespace MiTurno.Application.Features.Recursos.Bloqueos.Dtos;

public record BloqueoFechaResponse(
    Guid Id,
    Guid RecursoId,
    DateOnly Fecha,
    string? Motivo,
    IReadOnlyList<ReservaAfectadaResponse> ReservasAfectadas);
