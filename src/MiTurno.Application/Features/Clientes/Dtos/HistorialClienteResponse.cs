namespace MiTurno.Application.Features.Clientes.Dtos;

/// <summary>Datos de contacto de un cliente junto con su historial de reservas en el negocio.</summary>
public record HistorialClienteResponse(
    Guid Id,
    string Nombre,
    string Email,
    string? Telefono,
    IReadOnlyList<ReservaClienteResponse> Reservas);
