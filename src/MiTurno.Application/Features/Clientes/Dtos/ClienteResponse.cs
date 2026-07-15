namespace MiTurno.Application.Features.Clientes.Dtos;

/// <summary>Vista resumida de un cliente para el listado del dueño, con su actividad en el negocio.</summary>
public record ClienteResponse(
    Guid Id,
    string Nombre,
    string Email,
    string? Telefono,
    int TotalReservas,
    DateOnly UltimaReserva);
