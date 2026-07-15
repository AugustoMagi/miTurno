namespace MiTurno.Application.Common.Models;

/// <summary>Datos necesarios para notificarle por email al cliente un cambio de estado de su Reserva.</summary>
public record NotificacionReserva(
    string ClienteEmail,
    string ClienteNombre,
    string NegocioNombre,
    string RecursoNombre,
    DateOnly Fecha,
    TimeSpan HoraInicio,
    TimeSpan HoraFin);
