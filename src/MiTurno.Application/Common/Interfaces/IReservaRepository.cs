using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface IReservaRepository : IRepository<Reserva>
{
    /// <summary>Reservas de un recurso en una fecha, para validar solapamientos antes de confirmar una nueva.</summary>
    Task<IReadOnlyList<Reserva>> GetByRecursoYFechaAsync(Guid recursoId, DateOnly fecha, CancellationToken cancellationToken = default);
}
