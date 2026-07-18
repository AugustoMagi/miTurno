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

    public async Task<IReadOnlyList<Plan>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.ToListAsync(cancellationToken);

    public Task<Plan?> GetPlanDePruebaAsync(CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(p => p.EsPlanDePrueba, cancellationToken);
}
