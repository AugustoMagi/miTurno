using Microsoft.EntityFrameworkCore;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Domain.Common;

namespace MiTurno.Infrastructure.Persistence.Repositories;

public abstract class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly MiTurnoDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected Repository(MiTurnoDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);
}
