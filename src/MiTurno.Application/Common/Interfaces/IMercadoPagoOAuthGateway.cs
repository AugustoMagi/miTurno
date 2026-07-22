using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Common.Interfaces;

/// <summary>
/// Puerto hacia el flujo OAuth de Mercado Pago (distinto de IPagoGateway, que opera pagos ya
/// autenticados): canjea el código de autorización por tokens, los renueva, y resuelve el perfil
/// del usuario conectado. Nunca lanza excepciones ante fallas de red o rechazos de Mercado Pago.
/// </summary>
public interface IMercadoPagoOAuthGateway
{
    Task<Result<MercadoPagoOAuthTokenResult>> IntercambiarCodigoAsync(
        string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken = default);

    Task<Result<MercadoPagoOAuthTokenResult>> RefrescarTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default);

    Task<Result<MercadoPagoUsuarioResult>> ObtenerUsuarioAsync(
        string accessToken, CancellationToken cancellationToken = default);
}
