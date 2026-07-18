namespace MiTurno.Infrastructure.Pagos;

/// <summary>
/// Access Token de la propia cuenta de Mercado Pago de MiTurno, para cobrarle la suscripción SaaS
/// a los negocios (a diferencia de ConfiguracionPago, que es el Access Token de cada negocio para
/// cobrarles a sus propios clientes).
/// </summary>
public class MercadoPagoPlataformaSettings
{
    public const string SectionName = "MercadoPagoPlataforma";

    public string AccessToken { get; set; } = "";
}
