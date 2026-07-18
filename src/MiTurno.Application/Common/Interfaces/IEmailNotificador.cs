using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Common.Interfaces;

/// <summary>
/// Notifica por email al cliente los cambios de estado de su Reserva, y al dueño del negocio
/// cuando se confirma una nueva Reserva. Es "best effort": la implementación no debe lanzar si el
/// envío falla, para que un problema de la casilla de correo o del proveedor SMTP no eche atrás
/// una confirmación, un rechazo o una cancelación que ya se persistieron correctamente.
/// </summary>
public interface IEmailNotificador
{
    Task NotificarReservaConfirmadaAsync(NotificacionReserva notificacion, CancellationToken cancellationToken = default);

    Task NotificarReservaRechazadaAsync(NotificacionReserva notificacion, CancellationToken cancellationToken = default);

    Task NotificarReservaCanceladaAsync(NotificacionReserva notificacion, CancellationToken cancellationToken = default);

    Task NotificarNuevaReservaAlDuenioAsync(NotificacionNuevaReserva notificacion, CancellationToken cancellationToken = default);

    Task NotificarReservaCanceladaPorClienteAsync(NotificacionNuevaReserva notificacion, CancellationToken cancellationToken = default);
}
