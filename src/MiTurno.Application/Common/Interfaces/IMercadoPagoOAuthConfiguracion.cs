namespace MiTurno.Application.Common.Interfaces;

/// <summary>
/// Credenciales de la aplicación "Marketplace" que MiTurno registró en Mercado Pago Developers,
/// necesarias para el flujo OAuth (distintas del AccessToken de plataforma que cobra suscripciones).
/// </summary>
public interface IMercadoPagoOAuthConfiguracion
{
    string ClientId { get; }
    string ClientSecret { get; }

    /// <summary>Debe coincidir exactamente con la URL de redirección registrada en Mercado Pago Developers.</summary>
    string RedirectUri { get; }
}
