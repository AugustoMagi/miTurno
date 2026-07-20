using MiTurno.Application.Features.Perfil.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Perfil;

internal static class PerfilMapper
{
    public static MiPerfilResponse ToMiPerfilResponse(this Usuario usuario) => new(
        usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol.ToString());
}
