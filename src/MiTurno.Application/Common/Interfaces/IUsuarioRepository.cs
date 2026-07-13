using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface IUsuarioRepository : IRepository<Usuario>
{
    /// <summary>Usado para el login (el email es la credencial de acceso del usuario).</summary>
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
