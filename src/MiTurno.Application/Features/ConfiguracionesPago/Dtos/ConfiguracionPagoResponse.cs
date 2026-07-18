using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.ConfiguracionesPago.Dtos;

public record ConfiguracionPagoResponse(
    Guid Id,
    ProveedorPago Proveedor,
    string Alias,
    bool TieneAccessToken,
    bool Activo,
    DateTime FechaCreacion);
