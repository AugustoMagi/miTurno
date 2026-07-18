using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.ConfiguracionesPago.Dtos;

/// <summary>
/// Carga el dato de cobro del negocio (alias/CVU de Mercado Pago, link de pago de Stripe, etc.).
/// AccessToken es opcional: si se pega el propio Access Token de Mercado Pago del negocio, se activa
/// el cobro automático (Checkout Pro + webhook); sin él, el flujo sigue siendo 100% manual.
/// </summary>
public record ConectarConfiguracionPagoRequest(ProveedorPago Proveedor, string Alias, string? AccessToken = null);
