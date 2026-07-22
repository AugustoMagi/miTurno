using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Infrastructure.Pagos;

/// <summary>
/// Implementa IMercadoPagoOAuthGateway llamando directamente a la API REST de Mercado Pago (mismo
/// estilo que MercadoPagoGateway: sin SDK, nunca deja escapar una excepción de red/parseo).
/// </summary>
public class MercadoPagoOAuthGateway : IMercadoPagoOAuthGateway
{
    private const string BaseUrl = "https://api.mercadopago.com";

    private readonly HttpClient _httpClient;
    private readonly IMercadoPagoOAuthConfiguracion _oauthConfiguracion;

    public MercadoPagoOAuthGateway(HttpClient httpClient, IMercadoPagoOAuthConfiguracion oauthConfiguracion)
    {
        _httpClient = httpClient;
        _oauthConfiguracion = oauthConfiguracion;
    }

    public Task<Result<MercadoPagoOAuthTokenResult>> IntercambiarCodigoAsync(
        string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken = default) =>
        PedirTokenAsync(
            new
            {
                client_id = _oauthConfiguracion.ClientId,
                client_secret = _oauthConfiguracion.ClientSecret,
                grant_type = "authorization_code",
                code,
                redirect_uri = redirectUri,
                code_verifier = codeVerifier
            },
            cancellationToken);

    public Task<Result<MercadoPagoOAuthTokenResult>> RefrescarTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default) =>
        PedirTokenAsync(
            new
            {
                client_id = _oauthConfiguracion.ClientId,
                client_secret = _oauthConfiguracion.ClientSecret,
                grant_type = "refresh_token",
                refresh_token = refreshToken
            },
            cancellationToken);

    private async Task<Result<MercadoPagoOAuthTokenResult>> PedirTokenAsync(object body, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/oauth/token", body, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<MercadoPagoOAuthTokenResult>(
                    $"Mercado Pago rechazó la solicitud de token ({(int)response.StatusCode}).");

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
            if (string.IsNullOrEmpty(token?.AccessToken) || string.IsNullOrEmpty(token.RefreshToken))
                return Result.Failure<MercadoPagoOAuthTokenResult>("Respuesta inesperada de Mercado Pago al pedir el token.");

            return Result.Success(new MercadoPagoOAuthTokenResult(token.AccessToken, token.RefreshToken, token.ExpiresIn));
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<MercadoPagoOAuthTokenResult>($"No se pudo contactar a Mercado Pago: {ex.Message}");
        }
    }

    public async Task<Result<MercadoPagoUsuarioResult>> ObtenerUsuarioAsync(
        string accessToken, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/users/me");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<MercadoPagoUsuarioResult>(
                    $"Mercado Pago no devolvió el perfil del usuario ({(int)response.StatusCode}).");

            var usuario = await response.Content.ReadFromJsonAsync<UsuarioResponse>(cancellationToken);
            return Result.Success(new MercadoPagoUsuarioResult(usuario?.Email, usuario?.Nickname));
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<MercadoPagoUsuarioResult>($"No se pudo contactar a Mercado Pago: {ex.Message}");
        }
    }

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private record UsuarioResponse(string? Email, string? Nickname);
}
