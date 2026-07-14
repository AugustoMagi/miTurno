using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface IReservaRepository : IRepository<Reserva>
{
    /// <summary>Reservas de un recurso en una fecha, para validar solapamientos antes de confirmar una nueva.</summary>
    Task<IReadOnlyList<Reserva>> GetByRecursoYFechaAsync(Guid recursoId, DateOnly fecha, CancellationToken cancellationToken = default);

    /// <summary>Todas las reservas de los recursos de un negocio, para que el dueño las gestione.</summary>
    Task<IReadOnlyList<Reserva>> GetByNegocioIdAsync(Guid negocioId, CancellationToken cancellationToken = default);
}
