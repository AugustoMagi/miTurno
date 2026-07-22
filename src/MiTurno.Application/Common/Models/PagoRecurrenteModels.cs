using MiTurno.Domain.Enums;

namespace MiTurno.Application.Common.Models;

/// <summary>Datos para crear una suscripción recurrente (Preapproval) en Mercado Pago.</summary>
public record CrearPreapprovalRequest(
    string AccessToken,
    Guid ExternalReferenceId,
    string Razon,
    decimal Monto,
    Periodicidad Periodicidad,
    string PayerEmail,
    string BackUrl,
    string NotificationUrl);

public record PreapprovalCreadoResult(string PreapprovalId, string InitPoint);

/// <summary>Estado autoritativo de una Preapproval, reconsultado contra la API (nunca se confía en el webhook).</summary>
public record PreapprovalEstadoResult(string PreapprovalId, string Status, string? ExternalReference);

/// <summary>Un cargo puntual dentro de una suscripción recurrente (lo que Mercado Pago llama "authorized payment").</summary>
public record CargoRecurrenteResult(
    string PagoId, string PreapprovalId, decimal Monto, EstadoPagoExterno Estado);
