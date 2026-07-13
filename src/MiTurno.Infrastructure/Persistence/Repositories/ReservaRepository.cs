using Microsoft.EntityFrameworkCore;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Repositories;

public class ReservaRepository : Repository<Reserva>, IReservaRepository
{
    public ReservaRepository(MiTurnoDbContext context) : base(context)
    {
    }

    // Reserva.Pago es opcional (0..1): se incluye porque suele consultarse junto con la reserva.
    public override Task<Reserva?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet.Include(r => r.Pago).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reserva>> GetByRecursoYFechaAsync(Guid recursoId, DateOnly fecha, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(r => r.RecursoId == recursoId && r.Fecha == fecha)
            .ToListAsync(cancellationToken);
}
