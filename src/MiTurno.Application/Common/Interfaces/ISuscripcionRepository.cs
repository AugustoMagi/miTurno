using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface ISuscripcionRepository : IRepository<Suscripcion>
{
    /// <summary>La suscripción vigente de un negocio; de ella depende si su link público sigue expuesto.</summary>
    Task<Suscripcion?> GetByNegocioIdAsync(Guid negocioId, CancellationToken cancellationToken = default);

    /// <summary>Todas las suscripciones existentes, para la gestión del SysAdmin.</summary>
    Task<IReadOnlyList<Suscripcion>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Suscripciones activas o en prueba que vencen antes de <paramref name="hasta"/> y todavía no
    /// recibieron el aviso de vencimiento próximo (el flag se resetea al renovar).
    /// </summary>
    Task<IReadOnlyList<Suscripcion>> GetPendientesDeNotificarVencimientoAsync(DateTime hasta, CancellationToken cancellationToken = default);

    /// <summary>Si algún negocio tiene (o tuvo) una Suscripcion contra este Plan, no se lo puede eliminar.</summary>
    Task<bool> ExisteConPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
}
