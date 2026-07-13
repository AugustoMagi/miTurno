using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface IClienteRepository : IRepository<Cliente>
{
    /// <summary>El cliente no tiene login: se lo busca y se le acumula historial por email entre reservas.</summary>
    Task<Cliente?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
