using Microsoft.EntityFrameworkCore;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Repositories;

public class SysAdminRepository : Repository<SysAdmin>, ISysAdminRepository
{
    public SysAdminRepository(MiTurnoDbContext context) : base(context)
    {
    }

    public Task<SysAdmin?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(a => a.Email == email.ToLowerInvariant(), cancellationToken);
}
