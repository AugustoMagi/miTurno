using Microsoft.EntityFrameworkCore;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Repositories;

public class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    public ClienteRepository(MiTurnoDbContext context) : base(context)
    {
    }

    public Task<Cliente?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(c => c.Email == email.ToLowerInvariant(), cancellationToken);
}
