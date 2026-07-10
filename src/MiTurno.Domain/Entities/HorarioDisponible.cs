using MiTurno.Domain.Common;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

public class HorarioDisponible : BaseEntity
{
    public Guid RecursoId { get; private set; }
    public DayOfWeek DiaSemana { get; private set; }
    public TimeSpan HoraInicio { get; private set; }
    public TimeSpan HoraFin { get; private set; }

    private HorarioDisponible() { }

    public static HorarioDisponible Crear(Guid recursoId, DayOfWeek diaSemana, TimeSpan horaInicio, TimeSpan horaFin)
    {
        if (horaInicio >= horaFin)
            throw new DomainException("La hora de inicio debe ser anterior a la hora de fin.");

        return new HorarioDisponible
        {
            RecursoId = recursoId,
            DiaSemana = diaSemana,
            HoraInicio = horaInicio,
            HoraFin = horaFin
        };
    }
}
