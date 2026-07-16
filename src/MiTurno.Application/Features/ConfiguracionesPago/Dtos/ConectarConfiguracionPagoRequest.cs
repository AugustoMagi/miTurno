using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.ConfiguracionesPago.Dtos;

/// <summary>Carga el dato de cobro del negocio (alias/CVU de Mercado Pago, link de pago de Stripe, etc.).</summary>
public record ConectarConfiguracionPagoRequest(ProveedorPago Proveedor, string Alias);
