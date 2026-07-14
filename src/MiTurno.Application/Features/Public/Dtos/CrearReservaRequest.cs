namespace MiTurno.Application.Features.Public.Dtos;

/// <summary>Alta de una reserva por parte del cliente final, sobre un turno concreto de un recurso.</summary>
public record CrearReservaRequest(
    DateOnly Fecha,
    TimeSpan HoraInicio,
    string ClienteNombre,
    string ClienteEmail,
    string? ClienteTelefono);
