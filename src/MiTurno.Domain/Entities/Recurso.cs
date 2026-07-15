using MiTurno.Domain.Common;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

public class Recurso : BaseEntity
{
    public Guid NegocioId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Tipo { get; private set; } = null!;
    public TimeSpan DuracionTurno { get; private set; }
    public decimal Precio { get; private set; }
    public bool Activo { get; private set; }

    private readonly List<HorarioDisponible> _horariosDisponibles = [];
    public IReadOnlyCollection<HorarioDisponible> HorariosDisponibles => _horariosDisponibles.AsReadOnly();

    private readonly List<BloqueoFecha> _bloqueosFecha = [];
    public IReadOnlyCollection<BloqueoFecha> BloqueosFecha => _bloqueosFecha.AsReadOnly();

    private readonly List<Reserva> _reservas = [];
    public IReadOnlyCollection<Reserva> Reservas => _reservas.AsReadOnly();

    private Recurso() { }

    public static Recurso Crear(Guid negocioId, string nombre, string tipo, TimeSpan duracionTurno, decimal precio)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del recurso es obligatorio.");
        if (string.IsNullOrWhiteSpace(tipo))
            throw new DomainException("El tipo del recurso es obligatorio.");
        if (duracionTurno <= TimeSpan.Zero)
            throw new DomainException("La duración del turno debe ser mayor a cero.");
        if (precio < 0)
            throw new DomainException("El precio no puede ser negativo.");

        return new Recurso
        {
            NegocioId = negocioId,
            Nombre = nombre,
            Tipo = tipo,
            DuracionTurno = duracionTurno,
            Precio = precio,
            Activo = true
        };
    }

    public void AgregarHorarioDisponible(HorarioDisponible horario)
    {
        var seSuperpone = _horariosDisponibles.Any(h =>
            h.DiaSemana == horario.DiaSemana &&
            h.HoraInicio < horario.HoraFin &&
            horario.HoraInicio < h.HoraFin);

        if (seSuperpone)
            throw new DomainException("El horario se superpone con uno ya existente para ese día.");

        _horariosDisponibles.Add(horario);
    }

    public void EliminarHorarioDisponible(Guid horarioId)
    {
        var horario = _horariosDisponibles.FirstOrDefault(h => h.Id == horarioId);
        if (horario is null)
            throw new DomainException("El horario no existe para este recurso.");

        _horariosDisponibles.Remove(horario);
    }

    public void AgregarBloqueoFecha(BloqueoFecha bloqueo)
    {
        if (_bloqueosFecha.Any(b => b.Fecha == bloqueo.Fecha))
            throw new DomainException("Ya existe un bloqueo para esa fecha.");

        _bloqueosFecha.Add(bloqueo);
    }

    public void EliminarBloqueoFecha(Guid bloqueoId)
    {
        var bloqueo = _bloqueosFecha.FirstOrDefault(b => b.Id == bloqueoId);
        if (bloqueo is null)
            throw new DomainException("El bloqueo no existe para este recurso.");

        _bloqueosFecha.Remove(bloqueo);
    }

    public void ActualizarDatos(string nombre, string tipo, TimeSpan duracionTurno, decimal precio)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del recurso es obligatorio.");
        if (string.IsNullOrWhiteSpace(tipo))
            throw new DomainException("El tipo del recurso es obligatorio.");
        if (duracionTurno <= TimeSpan.Zero)
            throw new DomainException("La duración del turno debe ser mayor a cero.");
        if (precio < 0)
            throw new DomainException("El precio no puede ser negativo.");

        Nombre = nombre;
        Tipo = tipo;
        DuracionTurno = duracionTurno;
        Precio = precio;
        MarcarActualizado();
    }

    public void Desactivar()
    {
        Activo = false;
        MarcarActualizado();
    }

    public void Activar()
    {
        Activo = true;
        MarcarActualizado();
    }
}
