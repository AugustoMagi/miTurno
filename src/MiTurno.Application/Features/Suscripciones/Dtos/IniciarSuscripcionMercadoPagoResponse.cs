namespace MiTurno.Application.Features.Suscripciones.Dtos;

/// <summary>URL de Mercado Pago a la que el frontend debe redirigir al negocio para autorizar el cobro recurrente.</summary>
public record IniciarSuscripcionMercadoPagoResponse(string InitPoint);
