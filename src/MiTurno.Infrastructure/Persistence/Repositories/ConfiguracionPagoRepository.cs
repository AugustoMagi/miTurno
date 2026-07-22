using Microsoft.EntityFrameworkCore;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Repositories;

public class ConfiguracionPagoRepository : Repository<ConfiguracionPago>, IConfiguracionPagoRepository
{
    public ConfiguracionPagoRepository(MiTurnoDbContext context) : base(context)
    {
    }

    public Task<ConfiguracionPago?> GetActivaByNegocioIdAsync(Guid negocioId, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(c => c.NegocioId == negocioId && c.Activo, cancellationToken);

    public async Task<IReadOnlyList<ConfiguracionPago>> GetConexionesOAuthPorVencerAsync(
        DateTime antesDe, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(c => c.Activo && c.RefreshToken != null && c.AccessTokenExpiraEn <= antesDe)
            .ToListAsync(cancellationToken);
}
