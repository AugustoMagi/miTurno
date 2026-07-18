namespace MiTurno.Application.Common.Models;

/// <summary>Preferencia de Checkout Pro creada en Mercado Pago: LinkPago es el init_point para redirigir al cliente.</summary>
public record PreferenciaPagoResult(string PreferenciaId, string LinkPago);
