namespace MiTurno.Application.Features.Recursos.Bloqueos.Dtos;

/// <summary>Bloquea una fecha puntual de un recurso (feriado, mantenimiento, etc.) para que no acepte reservas.</summary>
public record AgregarBloqueoFechaRequest(DateOnly Fecha, string? Motivo);
