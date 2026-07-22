using Microsoft.EntityFrameworkCore;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Infrastructure.Persistence.Repositories;

public class SuscripcionRepository : Repository<Suscripcion>, ISuscripcionRepository
{
    public SuscripcionRepository(MiTurnoDbContext context) : base(context)
    {
    }

    // Suscripcion.Plan es una navegación requerida (no nula): siempre se incluye para no dejarla en
    // null al materializar la entidad. Pagos también se incluye siempre, para que agregar un nuevo
    // PagoSuscripcion a la colección y actualizar la Suscripcion lo persista correctamente.
    public override Task<Suscripcion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet.Include(s => s.Plan).Include(s => s.Pagos).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Suscripcion?> GetByNegocioIdAsync(Guid negocioId, CancellationToken cancellationToken = default) =>
        DbSet.Include(s => s.Plan).Include(s => s.Pagos).FirstOrDefaultAsync(s => s.NegocioId == negocioId, cancellationToken);

    // Se incluye Pagos también acá (y no solo en GetByIdAsync) porque el reporte de facturación del
    // SysAdmin necesita agregar PagoSuscripcion de todas las suscripciones a la vez.
    public async Task<IReadOnlyList<Suscripcion>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.Include(s => s.Plan).Include(s => s.Pagos).ToListAsync(cancellationToken);

    public Task<bool> ExisteConPlanIdAsync(Guid planId, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(s => s.PlanId == planId, cancellationToken);

    public async Task<IReadOnlyList<Suscripcion>> GetPendientesDeNotificarVencimientoAsync(
        DateTime hasta, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(s => s.Plan)
            .Where(s =>
                (s.Estado == EstadoSuscripcion.Activa || s.Estado == EstadoSuscripcion.EnPrueba)
                && !s.NotificacionVencimientoEnviada
                && s.FechaProximoVencimiento <= hasta)
            .ToListAsync(cancellationToken);
}
