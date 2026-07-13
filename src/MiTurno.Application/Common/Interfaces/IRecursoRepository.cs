using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface IRecursoRepository : IRepository<Recurso>
{
    /// <summary>Recursos (canchas) de un negocio, para listarlos en su página pública de reservas.</summary>
    Task<IReadOnlyList<Recurso>> GetByNegocioIdAsync(Guid negocioId, CancellationToken cancellationToken = default);

    /// <summary>Recurso con sus horarios y bloqueos cargados, necesarios para calcular turnos disponibles.</summary>
    Task<Recurso?> GetConHorariosYBloqueosAsync(Guid id, CancellationToken cancellationToken = default);
}
