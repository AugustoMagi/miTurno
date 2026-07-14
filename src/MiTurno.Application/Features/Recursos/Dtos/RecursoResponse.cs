namespace MiTurno.Application.Features.Recursos.Dtos;

public record RecursoResponse(
    Guid Id,
    Guid NegocioId,
    string Nombre,
    string Tipo,
    int DuracionTurnoMinutos,
    decimal Precio,
    bool Activo);
