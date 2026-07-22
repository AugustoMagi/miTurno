using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Admin.Negocios.Dtos;

public record NegocioDetalleAdminResponse(
    Guid Id,
    string Nombre,
    string Slug,
    string Email,
    bool Activo,
    IReadOnlyList<RecursoDetalleAdminResponse> Recursos);

public record RecursoDetalleAdminResponse(
    Guid Id,
    string Nombre,
    string Tipo,
    int DuracionTurnoMinutos,
    decimal Precio,
    bool Activo,
    IReadOnlyList<HorarioAdminResponse> Horarios,
    IReadOnlyList<ReservaAdminResponse> Reservas);

public record HorarioAdminResponse(Guid Id, DayOfWeek DiaSemana, TimeSpan HoraInicio, TimeSpan HoraFin);

public record ReservaAdminResponse(
    Guid Id,
    DateOnly Fecha,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    string ClienteNombre,
    string ClienteEmail,
    EstadoReserva Estado,
    decimal PrecioTotal);
