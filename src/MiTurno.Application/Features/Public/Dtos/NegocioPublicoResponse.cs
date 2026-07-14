namespace MiTurno.Application.Features.Public.Dtos;

/// <summary>Datos del negocio visibles desde su link público, para la página de reserva del cliente.</summary>
public record NegocioPublicoResponse(
    Guid Id,
    string Nombre,
    string Slug,
    string? Descripcion,
    string? Direccion,
    IReadOnlyList<RecursoPublicoResponse> Recursos);
