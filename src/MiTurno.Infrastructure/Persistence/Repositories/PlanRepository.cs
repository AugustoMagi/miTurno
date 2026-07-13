using Microsoft.EntityFrameworkCore;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Repositories;

public class PlanRepository : Repository<Plan>, IPlanRepository
{
    public PlanRepository(MiTurnoDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Plan>> GetActivosAsync(CancellationToken cancellationToken = default) =>
        await DbSet.Where(p => p.Activo).ToListAsync(cancellationToken);
}
