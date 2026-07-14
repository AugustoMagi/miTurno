namespace MiTurno.Application.Features.Recursos.Horarios.Dtos;

public record HorarioDisponibleResponse(
    Guid Id,
    Guid RecursoId,
    DayOfWeek DiaSemana,
    TimeSpan HoraInicio,
    TimeSpan HoraFin);
