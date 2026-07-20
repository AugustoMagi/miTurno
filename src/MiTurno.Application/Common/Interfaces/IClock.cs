namespace MiTurno.Application.Common.Interfaces;

/// <summary>
/// Da la hora actual en el huso horario de Argentina (UTC-3, sin horario de verano), que es el
/// que usan los horarios de negocio (HoraInicio/HoraFin) y las fechas de reserva: comparar contra
/// DateTime.UtcNow directamente rechaza "hoy" como fecha pasada apenas cruza medianoche UTC.
/// </summary>
public interface IClock
{
    DateTime Now { get; }
}
