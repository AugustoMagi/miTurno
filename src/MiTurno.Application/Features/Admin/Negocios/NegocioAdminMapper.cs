using MiTurno.Application.Features.Admin.Negocios.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Admin.Negocios;

internal static class NegocioAdminMapper
{
    public static NegocioAdminResponse ToResponse(this Negocio negocio) => new(
        negocio.Id,
        negocio.Nombre,
        negocio.Slug,
        negocio.Email,
        negocio.Telefono,
        negocio.Activo,
        negocio.FechaCreacion);
}
