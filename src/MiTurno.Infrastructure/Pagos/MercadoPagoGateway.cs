using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Infrastructure.Pagos;

/// <summary>
/// Implementa IPagoGateway llamando directamente a la API REST de Mercado Pago (sin SDK, primer uso
/// de HttpClient en el proyecto). Nunca deja escapar una excepción: cualquier falla de red o
/// respuesta no exitosa se traduce a Result.Failure, para que el llamador decida cómo degradar.
/// </summary>
public class MercadoPagoGateway : IPagoGateway
{
    private const string BaseUrl = "https://api.mercadopago.com";

    private readonly HttpClient _httpClient;

    public MercadoPagoGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<PreferenciaPagoResult>> CrearPreferenciaAsync(
        CrearPreferenciaPagoRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/checkout/preferences");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);
        httpRequest.Content = JsonContent.Create(new
        {
            items = new[]
            {
                new { title = request.Descripcion, quantity = 1, unit_price = request.Monto, currency_id = "ARS" }
            },
            external_reference = request.ReservaId.ToString(),
            notification_url = request.NotificationUrl
        });

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<PreferenciaPagoResult>(
                    $"Mercado Pago rechazó la creación de la preferencia ({(int)response.StatusCode}).");

            var body = await response.Content.ReadFromJsonAsync<PreferenciaResponse>(cancellationToken);
            if (string.IsNullOrEmpty(body?.Id) || string.IsNullOrEmpty(body.InitPoint))
                return Result.Failure<PreferenciaPagoResult>("Respuesta inesperada de Mercado Pago al crear la preferencia.");

            return Result.Success(new PreferenciaPagoResult(body.Id, body.InitPoint));
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<PreferenciaPagoResult>($"No se pudo contactar a Mercado Pago: {ex.Message}");
        }
    }

    public async Task<Result<EstadoPagoExternoResult>> ObtenerEstadoPagoAsync(
        string accessToken, string pagoExternoId, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v1/payments/{pagoExternoId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<EstadoPagoExternoResult>(
                    $"Mercado Pago no devolvió el pago {pagoExternoId} ({(int)response.StatusCode}).");

            var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(cancellationToken);
            if (body?.Status is null)
                return Result.Failure<EstadoPagoExternoResult>("Respuesta inesperada de Mercado Pago al consultar el pago.");

            var estado = body.Status switch
            {
                "approved" => EstadoPagoExterno.Aprobado,
                "rejected" or "cancelled" => EstadoPagoExterno.Rechazado,
                _ => EstadoPagoExterno.Pendiente
            };

            return Result.Success(new EstadoPagoExternoResult(pagoExternoId, estado, body.ExternalReference));
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<EstadoPagoExternoResult>($"No se pudo contactar a Mercado Pago: {ex.Message}");
        }
    }

    private record PreferenciaResponse(string? Id, [property: JsonPropertyName("init_point")] string? InitPoint);

    private record PaymentResponse(string? Status, [property: JsonPropertyName("external_reference")] string? ExternalReference);
}
