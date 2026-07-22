using MiTurno.Application.Features.Negocios.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Negocios;

internal static class NegocioMapper
{
    public static MiNegocioResponse ToMiNegocioResponse(this Negocio negocio) => new(
        negocio.Id,
        negocio.Nombre,
        negocio.Slug,
        negocio.Descripcion,
        negocio.Direccion,
        negocio.Telefono,
        negocio.Email,
        negocio.Activo);
}
