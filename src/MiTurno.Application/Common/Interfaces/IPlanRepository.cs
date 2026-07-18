using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface IPlanRepository : IRepository<Plan>
{
    /// <summary>Planes ofrecidos actualmente a nuevos negocios (catálogo público de precios).</summary>
    Task<IReadOnlyList<Plan>> GetActivosAsync(CancellationToken cancellationToken = default);

    /// <summary>Todos los planes, incluidos los inactivos, para la gestión del SysAdmin.</summary>
    Task<IReadOnlyList<Plan>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>El plan con el que arranca la Suscripcion de prueba de un negocio nuevo, si hay uno marcado.</summary>
    Task<Plan?> GetPlanDePruebaAsync(CancellationToken cancellationToken = default);
}
