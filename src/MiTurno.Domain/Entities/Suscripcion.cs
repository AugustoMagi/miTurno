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

    /// <summary>
    /// Id de la suscripción recurrente (Preapproval) de Mercado Pago que cobra este plan automáticamente
    /// a la cuenta de la propia plataforma. Null si el negocio nunca activó el cobro automático (paga
    /// manualmente o vía Admin).
    /// </summary>
    public string? MercadoPagoPreapprovalId { get; private set; }

    /// <summary>
    /// Si hay Preapproval asignada pero está pausada del lado de Mercado Pago: no cobra, pero a
    /// diferencia de cancelarla del todo, se puede reanudar con un solo PUT (sin que el negocio
    /// tenga que volver a autorizar el pago desde el checkout de Mercado Pago).
    /// </summary>
    public bool CobroAutomaticoPausado { get; private set; }

    /// <summary>
    /// Plan al que se está cambiando, todavía sin confirmar el pago. Mientras esto no sea null, el
    /// plan/la Preapproval vigentes (arriba) siguen intactos a propósito: si el negocio no llega a
    /// pagar el plan nuevo, no tiene que perder el que ya tenía funcionando.
    /// </summary>
    public Guid? PlanPendienteId { get; private set; }

    /// <summary>Preapproval del plan pendiente de confirmar (ver PlanPendienteId) — todavía no es la vigente.</summary>
    public string? MercadoPagoPreapprovalIdPendiente { get; private set; }

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
    /// Cancelada también cuenta como activa mientras no se cumpla la fecha de vencimiento: cancelar
    /// (sea desde MiTurno, desde la propia cuenta de Mercado Pago, o por un SysAdmin) sólo apaga la
    /// renovación automática, no corta el acceso al período ya pago. Vencida es el único estado que
    /// bloquea sin importar la fecha.
    /// </summary>
    public bool EstaActiva =>
        Estado != EstadoSuscripcion.Vencida
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

    public void AsignarPreapproval(string preapprovalId)
    {
        if (string.IsNullOrWhiteSpace(preapprovalId))
            throw new DomainException("El id de la suscripción de Mercado Pago es obligatorio.");

        MercadoPagoPreapprovalId = preapprovalId;
        MarcarActualizado();
    }

    public void QuitarPreapproval()
    {
        MercadoPagoPreapprovalId = null;
        CobroAutomaticoPausado = false;
        MarcarActualizado();
    }

    /// <summary>
    /// Pausa el cobro automático sin perder la autorización: a diferencia de cancelar la Preapproval
    /// del todo, se puede reanudar después sin que el negocio pase de nuevo por el checkout de
    /// Mercado Pago.
    /// </summary>
    public void PausarCobroAutomatico()
    {
        if (MercadoPagoPreapprovalId is null)
            throw new DomainException("No hay cobro automático para pausar.");

        CobroAutomaticoPausado = true;
        MarcarActualizado();
    }

    /// <summary>
    /// Reanuda un cobro automático pausado: usa la misma Preapproval ya autorizada, así que no hace
    /// falta que el negocio vuelva a pasar por Mercado Pago.
    /// </summary>
    public void ReanudarCobroAutomatico()
    {
        if (MercadoPagoPreapprovalId is null)
            throw new DomainException("No hay cobro automático para reanudar.");

        CobroAutomaticoPausado = false;
        Estado = EstadoSuscripcion.Activa;
        MarcarActualizado();
    }

    public void CambiarPlan(Plan nuevoPlan)
    {
        PlanId = nuevoPlan.Id;
        Plan = nuevoPlan;
        MarcarActualizado();
    }

    /// <summary>
    /// Arranca un cambio de plan pago: a propósito no toca ni el plan ni la Preapproval vigentes
    /// todavía — eso recién pasa en ConfirmarCambioDePlanPendiente, cuando se confirma que esta
    /// Preapproval nueva quedó autorizada. Así, si el negocio entra a Mercado Pago y no llega a
    /// pagar, el plan y el cobro automático que ya tenía siguen funcionando sin cambios.
    /// </summary>
    public void IniciarCambioDePlanConPago(Guid planPendienteId, string preapprovalIdPendiente)
    {
        if (string.IsNullOrWhiteSpace(preapprovalIdPendiente))
            throw new DomainException("El id de la Preapproval pendiente es obligatorio.");

        PlanPendienteId = planPendienteId;
        MercadoPagoPreapprovalIdPendiente = preapprovalIdPendiente;
        MarcarActualizado();
    }

    /// <summary>
    /// Confirma un cambio de plan pendiente: recién acá pasa a ser el plan y la Preapproval vigentes,
    /// una vez que se verificó que el pago del plan nuevo se autorizó de verdad.
    /// </summary>
    public void ConfirmarCambioDePlanPendiente(Plan planConfirmado)
    {
        if (PlanPendienteId != planConfirmado.Id || MercadoPagoPreapprovalIdPendiente is null)
            throw new DomainException("No hay un cambio de plan pendiente que coincida con ese plan.");

        PlanId = planConfirmado.Id;
        Plan = planConfirmado;
        MercadoPagoPreapprovalId = MercadoPagoPreapprovalIdPendiente;
        CobroAutomaticoPausado = false;
        PlanPendienteId = null;
        MercadoPagoPreapprovalIdPendiente = null;
        MarcarActualizado();
    }

    /// <summary>
    /// Descarta un cambio de plan pendiente que no llegó a confirmarse (el negocio no pagó, o la
    /// Preapproval nueva quedó cancelada en Mercado Pago): el plan y el cobro automático vigentes no
    /// se tocaron en ningún momento, así que no hay nada que revertir más que limpiar estos campos.
    /// </summary>
    public void DescartarCambioDePlanPendiente()
    {
        PlanPendienteId = null;
        MercadoPagoPreapprovalIdPendiente = null;
        MarcarActualizado();
    }
}
