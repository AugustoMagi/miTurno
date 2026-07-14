namespace MiTurno.Application.Features.Public.Dtos;

public record RecursoPublicoResponse(
    Guid Id,
    string Nombre,
    string Tipo,
    int DuracionTurnoMinutos,
    decimal Precio);
