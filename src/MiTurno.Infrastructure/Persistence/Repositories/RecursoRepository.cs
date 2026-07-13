using Microsoft.EntityFrameworkCore;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Repositories;

public class RecursoRepository : Repository<Recurso>, IRecursoRepository
{
    public RecursoRepository(MiTurnoDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Recurso>> GetByNegocioIdAsync(Guid negocioId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(r => r.NegocioId == negocioId).ToListAsync(cancellationToken);

    public Task<Recurso?> GetConHorariosYBloqueosAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(r => r.HorariosDisponibles)
            .Include(r => r.BloqueosFecha)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
}
