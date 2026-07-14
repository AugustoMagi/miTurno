using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Reservas.Dtos;

/// <summary>Vista de una reserva para el dueño del negocio, con los datos del recurso y del cliente ya resueltos.</summary>
public record ReservaOwnerResponse(
    Guid Id,
    Guid RecursoId,
    string RecursoNombre,
    Guid ClienteId,
    string ClienteNombre,
    string ClienteEmail,
    DateOnly Fecha,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    decimal PrecioTotal,
    EstadoReserva Estado);
