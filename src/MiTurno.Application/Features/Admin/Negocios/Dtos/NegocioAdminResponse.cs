namespace MiTurno.Application.Features.Admin.Negocios.Dtos;

public record NegocioAdminResponse(
    Guid Id,
    string Nombre,
    string Slug,
    string Email,
    string? Telefono,
    bool Activo,
    DateTime FechaCreacion);
