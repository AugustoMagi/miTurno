namespace MiTurno.Application.Features.Recursos.Bloqueos.Dtos;

/// <summary>
/// Bloquea una fecha puntual de un recurso para que no acepte reservas. Si HoraInicio/HoraFin
/// vienen nulos, bloquea el día completo (feriado, mantenimiento); si vienen cargados, bloquea solo
/// ese rango horario (ej. el dueño ya lo reservó por fuera de MiTurno, por teléfono o WhatsApp).
/// </summary>
public record AgregarBloqueoFechaRequest(
    DateOnly Fecha, string? Motivo, TimeSpan? HoraInicio = null, TimeSpan? HoraFin = null);
