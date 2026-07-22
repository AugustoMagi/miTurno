namespace MiTurno.Application.Features.Negocios.Dtos;

public record MiNegocioResponse(
    Guid Id,
    string Nombre,
    string Slug,
    string? Descripcion,
    string? Direccion,
    string? Telefono,
    string Email,
    bool Activo);
