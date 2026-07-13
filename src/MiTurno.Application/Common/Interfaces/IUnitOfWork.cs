namespace MiTurno.Application.Common.Interfaces;

/// <summary>
/// Confirma en una sola transacción los cambios hechos a través de uno o más repositorios
/// durante un caso de uso, evitando que cada repositorio persista por su cuenta.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
