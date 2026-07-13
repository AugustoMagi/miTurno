using MiTurno.Application.Common.Interfaces;

namespace MiTurno.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly MiTurnoDbContext _context;

    public UnitOfWork(MiTurnoDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
