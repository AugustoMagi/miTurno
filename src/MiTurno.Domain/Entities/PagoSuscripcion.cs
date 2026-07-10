using MiTurno.Domain.Common;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

public class PagoSuscripcion : BaseEntity
{
    public Guid SuscripcionId { get; private set; }
    public decimal Monto { get; private set; }
    public DateTime Fecha { get; private set; }
    public EstadoPago Estado { get; private set; }
    public string? TransaccionExternalId { get; private set; }

    private PagoSuscripcion() { }

    public static PagoSuscripcion Registrar(Guid suscripcionId, decimal monto, string? transaccionExternalId)
    {
        if (monto <= 0)
            throw new DomainException("El monto del pago debe ser mayor a cero.");

        return new PagoSuscripcion
        {
            SuscripcionId = suscripcionId,
            Monto = monto,
            Fecha = DateTime.UtcNow,
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
}
