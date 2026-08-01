using MiTurno.Domain.Common;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

/// <summary>
/// Bloquea la disponibilidad de un recurso en una fecha puntual: el día completo (HoraInicio y
/// HoraFin nulos, ej. feriado, mantenimiento) o solo un rango horario dentro de ese día (ej. el
/// dueño ya lo comprometió por teléfono o WhatsApp y quiere que no se pueda reservar online).
/// </summary>
public class BloqueoFecha : BaseEntity
{
    public Guid RecursoId { get; private set; }
    public DateOnly Fecha { get; private set; }
    public TimeSpan? HoraInicio { get; private set; }
    public TimeSpan? HoraFin { get; private set; }
    public string? Motivo { get; private set; }

    public bool EsDiaCompleto => HoraInicio is null;

    private BloqueoFecha() { }

    public static BloqueoFecha Crear(
        Guid recursoId, DateOnly fecha, TimeSpan? horaInicio = null, TimeSpan? horaFin = null, string? motivo = null)
    {
        if ((horaInicio is null) != (horaFin is null))
            throw new DomainException("Si cargás un horario de bloqueo, completá tanto el inicio como el fin.");
        if (horaInicio is not null && horaInicio >= horaFin)
            throw new DomainException("El horario \"desde\" del bloqueo debe ser anterior al \"hasta\".");

        return new BloqueoFecha
        {
            RecursoId = recursoId,
            Fecha = fecha,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            Motivo = motivo
        };
    }

    /// <summary>Dos bloqueos con horario se superponen si sus rangos se cruzan; no aplica a bloqueos de día completo.</summary>
    public bool SeSuperponeCon(BloqueoFecha otro) =>
        HoraInicio!.Value < otro.HoraFin!.Value && otro.HoraInicio!.Value < HoraFin!.Value;

    /// <summary>Si el turno [inicio, fin) cae dentro de este bloqueo (día completo, o superpuesto con el horario bloqueado).</summary>
    public bool Cubre(TimeSpan inicio, TimeSpan fin) =>
        EsDiaCompleto || (HoraInicio!.Value < fin && inicio < HoraFin!.Value);
}
