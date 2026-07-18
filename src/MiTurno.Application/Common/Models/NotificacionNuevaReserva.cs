namespace MiTurno.Application.Common.Models;

/// <summary>Datos necesarios para avisarle por email al dueño del negocio que se confirmó una nueva Reserva.</summary>
public record NotificacionNuevaReserva(
    string NegocioEmail,
    string NegocioNombre,
    string ClienteNombre,
    string RecursoNombre,
    DateOnly Fecha,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    decimal PrecioTotal);
