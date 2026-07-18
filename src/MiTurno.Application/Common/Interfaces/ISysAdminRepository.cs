using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface ISysAdminRepository : IRepository<SysAdmin>
{
    /// <summary>Usado para el login (el email es la credencial de acceso del administrador).</summary>
    Task<SysAdmin?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
