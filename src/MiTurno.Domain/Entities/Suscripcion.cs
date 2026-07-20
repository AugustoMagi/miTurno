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
    public bool NotificacionVencimientoEnviada { get; private set; }

    private readonly List<PagoSuscripcion> _pagos = [];
    public IReadOnlyCollection<PagoSuscripcion> Pagos => _pagos.AsReadOnly();

    private Suscripcion() { }

    public static Suscripcion IniciarPrueba(Guid negocioId, Plan plan, int diasPrueba = 14)
    {
        if (diasPrueba <= 0)
            throw new DomainException("Los días de prueba deben ser mayores a cero.");

        var ahora = DateTime.UtcNow;
        return new Suscripcion
        {
            NegocioId = negocioId,
            PlanId = plan.Id,
            Plan = plan,
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

    /// <summary>
    /// Renueva manualmente el vencimiento y deja la suscripción en Activa, sea cual sea su estado
    /// anterior — incluida Cancelada: es también el mecanismo por el que un SysAdmin reactiva una
    /// suscripción cancelada por error o a pedido del negocio, sin necesitar una acción separada.
    /// </summary>
    public void Renovar(DateTime nuevoVencimiento)
    {
        if (nuevoVencimiento <= FechaProximoVencimiento)
            throw new DomainException("La nueva fecha de vencimiento debe ser posterior a la actual.");

        Estado = EstadoSuscripcion.Activa;
        FechaProximoVencimiento = nuevoVencimiento;
        NotificacionVencimientoEnviada = false;
        MarcarActualizado();
    }

    public void MarcarNotificacionVencimientoEnviada()
    {
        NotificacionVencimientoEnviada = true;
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

    public void CambiarPlan(Plan nuevoPlan)
    {
        PlanId = nuevoPlan.Id;
        Plan = nuevoPlan;
        MarcarActualizado();
    }
}
