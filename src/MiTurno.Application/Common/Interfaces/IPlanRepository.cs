using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface IPlanRepository : IRepository<Plan>
{
    /// <summary>Planes ofrecidos actualmente a nuevos negocios (catálogo público de precios).</summary>
    Task<IReadOnlyList<Plan>> GetActivosAsync(CancellationToken cancellationToken = default);
}
