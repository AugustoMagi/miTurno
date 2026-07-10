using MiTurno.Domain.Common;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

public class Reserva : BaseEntity
{
    public Guid RecursoId { get; private set; }
    public Guid ClienteId { get; private set; }
    public DateOnly Fecha { get; private set; }
    public TimeSpan HoraInicio { get; private set; }
    public TimeSpan HoraFin { get; private set; }
    public EstadoReserva Estado { get; private set; }
    public decimal PrecioTotal { get; private set; }

    public Pago? Pago { get; private set; }

    private Reserva() { }

    public static Reserva Crear(Guid recursoId, Guid clienteId, DateOnly fecha, TimeSpan horaInicio, TimeSpan horaFin, decimal precioTotal)
    {
        if (horaInicio >= horaFin)
            throw new DomainException("La hora de inicio debe ser anterior a la hora de fin.");
        if (precioTotal < 0)
            throw new DomainException("El precio total no puede ser negativo.");

        return new Reserva
        {
            RecursoId = recursoId,
            ClienteId = clienteId,
            Fecha = fecha,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            PrecioTotal = precioTotal,
            Estado = EstadoReserva.Pendiente
        };
    }

    public void Confirmar()
    {
        if (Estado != EstadoReserva.Pendiente)
            throw new DomainException($"No se puede confirmar una reserva en estado {Estado}.");

        Estado = EstadoReserva.Confirmada;
        MarcarActualizado();
    }

    public void Cancelar()
    {
        if (Estado == EstadoReserva.Completada)
            throw new DomainException("No se puede cancelar una reserva ya completada.");
        if (Estado == EstadoReserva.Cancelada)
            throw new DomainException("La reserva ya está cancelada.");

        Estado = EstadoReserva.Cancelada;
        MarcarActualizado();
    }

    public void Completar()
    {
        if (Estado != EstadoReserva.Confirmada)
            throw new DomainException("Solo se puede completar una reserva confirmada.");

        Estado = EstadoReserva.Completada;
        MarcarActualizado();
    }

    public void AsignarPago(Pago pago) => Pago = pago;
}
