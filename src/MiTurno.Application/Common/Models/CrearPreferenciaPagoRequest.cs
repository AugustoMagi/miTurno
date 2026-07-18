namespace MiTurno.Application.Common.Models;

/// <summary>Datos para crear una preferencia de Checkout Pro en Mercado Pago para una Reserva concreta.</summary>
public record CrearPreferenciaPagoRequest(
    string AccessToken,
    Guid ReservaId,
    string Descripcion,
    decimal Monto,
    string NotificationUrl);
