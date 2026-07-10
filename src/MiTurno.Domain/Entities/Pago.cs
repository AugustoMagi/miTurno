using MiTurno.Domain.Common;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

/// <summary>
/// Pago de una Reserva (Cliente -> Negocio). Se acredita mediante el proveedor configurado
/// en la ConfiguracionPago del propio Negocio, por lo que no repite el campo Proveedor.
/// </summary>
public class Pago : BaseEntity
{
    public Guid ReservaId { get; private set; }
    public decimal Monto { get; private set; }
    public EstadoPago Estado { get; private set; }
    public string? TransaccionExternalId { get; private set; }

    private Pago() { }

    public static Pago Registrar(Guid reservaId, decimal monto, string? transaccionExternalId = null)
    {
        if (monto <= 0)
            throw new DomainException("El monto del pago debe ser mayor a cero.");

        return new Pago
        {
            ReservaId = reservaId,
            Monto = monto,
            Estado = EstadoPago.Pendiente,
            TransaccionExternalId = transaccionExternalId
        };
    }

    public void Aprobar()
    {
        if (Estado != EstadoPago.Pendiente)
            throw new DomainException($"No se puede aprobar un pago en estado {Estado}.");

        Estado = EstadoPago.Aprobado;
        MarcarActualizado();
    }

    public void Rechazar()
    {
        if (Estado != EstadoPago.Pendiente)
            throw new DomainException($"No se puede rechazar un pago en estado {Estado}.");

        Estado = EstadoPago.Rechazado;
        MarcarActualizado();
    }

    public void Reembolsar()
    {
        if (Estado != EstadoPago.Aprobado)
            throw new DomainException("Solo se puede reembolsar un pago aprobado.");

        Estado = EstadoPago.Reembolsado;
        MarcarActualizado();
    }
}
