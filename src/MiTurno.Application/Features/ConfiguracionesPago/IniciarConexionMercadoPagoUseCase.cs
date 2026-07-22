using MiTurno.Application.Common.Interfaces;

namespace MiTurno.Application.Features.ConfiguracionesPago;

/// <summary>
/// Arma la URL de autorización de Mercado Pago para que el dueño conecte su cuenta por OAuth: genera
/// el par PKCE (code_verifier/code_challenge) y empaqueta negocioId + code_verifier en un "state"
/// cifrado, para no depender de guardar nada en base entre este paso y el callback.
/// </summary>
public class IniciarConexionMercadoPagoUseCase
{
    private const string AuthorizationBaseUrl = "https://auth.mercadopago.com.ar/authorization";

    private readonly IMercadoPagoOAuthConfiguracion _oauthConfiguracion;
    private readonly IEstadoOAuthProtector _estadoOAuthProtector;

    public IniciarConexionMercadoPagoUseCase(
        IMercadoPagoOAuthConfiguracion oauthConfiguracion, IEstadoOAuthProtector estadoOAuthProtector)
    {
        _oauthConfiguracion = oauthConfiguracion;
        _estadoOAuthProtector = estadoOAuthProtector;
    }

    public Task<string> ExecuteAsync(Guid negocioId, CancellationToken cancellationToken = default)
    {
        var codeVerifier = PkceGenerator.GenerarCodeVerifier();
        var codeChallenge = PkceGenerator.CalcularCodeChallenge(codeVerifier);
        var state = _estadoOAuthProtector.Proteger(negocioId, codeVerifier);

        var url =
            $"{AuthorizationBaseUrl}?client_id={Uri.EscapeDataString(_oauthConfiguracion.ClientId)}" +
            "&response_type=code" +
            "&platform_id=mp" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&redirect_uri={Uri.EscapeDataString(_oauthConfiguracion.RedirectUri)}" +
            $"&code_challenge={codeChallenge}" +
            "&code_challenge_method=S256";

        return Task.FromResult(url);
    }
}
