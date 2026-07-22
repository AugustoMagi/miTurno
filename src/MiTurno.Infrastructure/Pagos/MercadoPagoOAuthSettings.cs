namespace MiTurno.Infrastructure.Pagos;

/// <summary>Credenciales de la aplicación "Marketplace" que MiTurno registró en Mercado Pago Developers.</summary>
public class MercadoPagoOAuthSettings
{
    public const string SectionName = "MercadoPagoOAuth";

    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RedirectUri { get; set; } = "";
}
