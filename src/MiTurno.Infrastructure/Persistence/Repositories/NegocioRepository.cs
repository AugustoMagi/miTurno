using Microsoft.EntityFrameworkCore;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Repositories;

public class NegocioRepository : Repository<Negocio>, INegocioRepository
{
    public NegocioRepository(MiTurnoDbContext context) : base(context)
    {
    }

    public Task<Negocio?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(n => n.Slug == slug.ToLowerInvariant(), cancellationToken);

    public async Task<IReadOnlyList<Negocio>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.OrderBy(n => n.Nombre).ToListAsync(cancellationToken);
}
