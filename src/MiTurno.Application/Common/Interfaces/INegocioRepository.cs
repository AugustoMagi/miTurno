using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface INegocioRepository : IRepository<Negocio>
{
    /// <summary>Resuelve el negocio a partir del slug de su link público de reservas.</summary>
    Task<Negocio?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
