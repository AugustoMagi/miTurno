using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Clientes.Dtos;

public record ReservaClienteResponse(
    Guid Id,
    string RecursoNombre,
    DateOnly Fecha,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    decimal PrecioTotal,
    EstadoReserva Estado);
