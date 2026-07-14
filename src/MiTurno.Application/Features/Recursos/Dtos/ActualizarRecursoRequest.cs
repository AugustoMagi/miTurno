namespace MiTurno.Application.Features.Recursos.Dtos;

public record ActualizarRecursoRequest(
    string Nombre,
    string Tipo,
    int DuracionTurnoMinutos,
    decimal Precio);
