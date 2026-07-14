using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerarToken(Usuario usuario);
}
