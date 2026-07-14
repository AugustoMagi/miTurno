namespace MiTurno.Application.Features.Recursos.Dtos;

/// <summary>Alta de un recurso (cancha) dentro del negocio del usuario autenticado.</summary>
public record CrearRecursoRequest(
    string Nombre,
    string Tipo,
    int DuracionTurnoMinutos,
    decimal Precio);
