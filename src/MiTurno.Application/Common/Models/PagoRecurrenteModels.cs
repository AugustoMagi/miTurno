using MiTurno.Domain.Enums;

namespace MiTurno.Application.Common.Models;

/// <summary>Datos para crear una suscripción recurrente (Preapproval) en Mercado Pago.</summary>
/// <param name="FechaInicio">
/// Null para que el primer cobro salga ya mismo (no se manda start_date: calcular "ahora" acá y
/// mandarlo corre el riesgo de que, por la latencia de red, ya esté en el pasado para cuando Mercado
/// Pago lo valida, y lo rechace con "cannot be a past date"). Con fecha, para diferir el primer cobro
/// a ese momento (ej. el vencimiento de la prueba gratis vigente).
/// </param>
public record CrearPreapprovalRequest(
    string AccessToken,
    Guid ExternalReferenceId,
    string Razon,
    decimal Monto,
    Periodicidad Periodicidad,
    string PayerEmail,
    string BackUrl,
    string NotificationUrl,
    DateTime? FechaInicio);

public record PreapprovalCreadoResult(string PreapprovalId, string InitPoint);

/// <summary>Estado autoritativo de una Preapproval, reconsultado contra la API (nunca se confía en el webhook).</summary>
public record PreapprovalEstadoResult(string PreapprovalId, string Status, string? ExternalReference);

/// <summary>Un cargo puntual dentro de una suscripción recurrente (lo que Mercado Pago llama "authorized payment").</summary>
public record CargoRecurrenteResult(
    string PagoId, string PreapprovalId, decimal Monto, EstadoPagoExterno Estado);
