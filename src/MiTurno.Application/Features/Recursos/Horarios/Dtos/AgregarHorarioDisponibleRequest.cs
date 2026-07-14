namespace MiTurno.Application.Features.Recursos.Horarios.Dtos;

/// <summary>Franja horaria semanal recurrente en la que un recurso acepta turnos.</summary>
public record AgregarHorarioDisponibleRequest(
    DayOfWeek DiaSemana,
    TimeSpan HoraInicio,
    TimeSpan HoraFin);
