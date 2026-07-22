using MiTurno.Application.Features.ConfiguracionesPago.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.ConfiguracionesPago;

internal static class ConfiguracionPagoMapper
{
    public static ConfiguracionPagoResponse ToResponse(this ConfiguracionPago configuracion) => new(
        configuracion.Id,
        configuracion.Proveedor,
        configuracion.Alias,
        configuracion.AccessToken is not null,
        configuracion.RefreshToken is not null,
        configuracion.Activo,
        configuracion.FechaCreacion);
}
