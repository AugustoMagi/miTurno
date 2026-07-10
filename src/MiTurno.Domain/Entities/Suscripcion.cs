using MiTurno.Domain.Common;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

public class Suscripcion : BaseEntity
{
    public Guid NegocioId { get; private set; }
    public Guid PlanId { get; private set; }
    public Plan Plan { get; private set; } = null!;
    public EstadoSuscripcion Estado { get; private set; }
    public DateTime FechaInicio { get; private set; }
    public DateTime FechaProximoVencimiento { get; private set; }

    private readonly List<PagoSuscripcion> _pagos = [];
    public IReadOnlyCollection<PagoSuscripcion> Pagos => _pagos.AsReadOnly();

    private Suscripcion() { }

    public static Suscripcion IniciarPrueba(Guid negocioId, Guid planId, int diasPrueba = 14)
    {
        if (diasPrueba <= 0)
            throw new DomainException("Los días de prueba deben ser mayores a cero.");

        var ahora = DateTime.UtcNow;
        return new Suscripcion
        {
            NegocioId = negocioId,
            PlanId = planId,
            Estado = EstadoSuscripcion.EnPrueba,
            FechaInicio = ahora,
            FechaProximoVencimiento = ahora.AddDays(diasPrueba)
        };
    }

    /// <summary>
    /// Determina si el negocio conserva acceso al sistema (prueba o pago vigente).
    /// De esto depende si su link público de reservas sigue expuesto.
    /// </summary>
    public bool EstaActiva =>
        Estado is EstadoSuscripcion.Activa or EstadoSuscripcion.EnPrueba
        && FechaProximoVencimiento >= DateTime.UtcNow;

    public void Renovar(DateTime nuevoVencimiento)
    {
        if (Estado == EstadoSuscripcion.Cancelada)
            throw new DomainException("No se puede renovar una suscripción cancelada.");
        if (nuevoVencimiento <= FechaProximoVencimiento)
            throw new DomainException("La nueva fecha de vencimiento debe ser posterior a la actual.");

        Estado = EstadoSuscripcion.Activa;
        FechaProximoVencimiento = nuevoVencimiento;
        MarcarActualizado();
    }

    public void MarcarVencida()
    {
        if (Estado == EstadoSuscripcion.Cancelada)
            return;

        Estado = EstadoSuscripcion.Vencida;
        MarcarActualizado();
    }

    public void Cancelar()
    {
        Estado = EstadoSuscripcion.Cancelada;
        MarcarActualizado();
    }

    public void RegistrarPago(PagoSuscripcion pago) => _pagos.Add(pago);
}
