using MiTurno.Domain.Common;

namespace MiTurno.Domain.Entities;

public class BloqueoFecha : BaseEntity
{
    public Guid RecursoId { get; private set; }
    public DateOnly Fecha { get; private set; }
    public string? Motivo { get; private set; }

    private BloqueoFecha() { }

    public static BloqueoFecha Crear(Guid recursoId, DateOnly fecha, string? motivo = null)
    {
        return new BloqueoFecha
        {
            RecursoId = recursoId,
            Fecha = fecha,
            Motivo = motivo
        };
    }
}
