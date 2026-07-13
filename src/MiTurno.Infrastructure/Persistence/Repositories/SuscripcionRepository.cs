using Microsoft.EntityFrameworkCore;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Repositories;

public class SuscripcionRepository : Repository<Suscripcion>, ISuscripcionRepository
{
    public SuscripcionRepository(MiTurnoDbContext context) : base(context)
    {
    }

    // Suscripcion.Plan es una navegación requerida (no nula): siempre se incluye para
    // no dejarla en null al materializar la entidad.
    public override Task<Suscripcion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet.Include(s => s.Plan).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Suscripcion?> GetByNegocioIdAsync(Guid negocioId, CancellationToken cancellationToken = default) =>
        DbSet.Include(s => s.Plan).FirstOrDefaultAsync(s => s.NegocioId == negocioId, cancellationToken);
}
