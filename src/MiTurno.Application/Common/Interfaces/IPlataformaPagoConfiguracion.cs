namespace MiTurno.Application.Common.Interfaces;

/// <summary>
/// Access Token de la cuenta de Mercado Pago de la propia plataforma MiTurno (cobro de la
/// suscripción SaaS a los negocios), distinto del AccessToken por-negocio de ConfiguracionPago.
/// </summary>
public interface IPlataformaPagoConfiguracion
{
    string AccessToken { get; }
}
