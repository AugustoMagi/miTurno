using Microsoft.EntityFrameworkCore;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Repositories;

public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(MiTurnoDbContext context) : base(context)
    {
    }

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
}
