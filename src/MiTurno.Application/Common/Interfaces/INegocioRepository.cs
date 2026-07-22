using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface INegocioRepository : IRepository<Negocio>
{
    /// <summary>Resuelve el negocio a partir del slug de su link público de reservas.</summary>
    Task<Negocio?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Todos los negocios de la plataforma, para la gestión del SysAdmin.</summary>
    Task<IReadOnlyList<Negocio>> GetAllAsync(CancellationToken cancellationToken = default);
}
