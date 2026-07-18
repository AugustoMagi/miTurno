namespace MiTurno.Application.Common.Models;

/// <summary>
/// Datos para crear una preferencia de Checkout Pro en Mercado Pago. ExternalReferenceId identifica
/// del lado de MiTurno qué se está cobrando (el id de una Reserva o de un PagoSuscripcion, según
/// quién llame) para poder matchearlo de vuelta cuando llega la notificación del pago.
/// </summary>
public record CrearPreferenciaPagoRequest(
    string AccessToken,
    Guid ExternalReferenceId,
    string Descripcion,
    decimal Monto,
    string NotificationUrl);
