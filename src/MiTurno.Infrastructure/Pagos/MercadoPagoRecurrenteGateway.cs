using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Domain.Enums;

namespace MiTurno.Infrastructure.Pagos;

/// <summary>
/// Implementa IPagoRecurrenteGateway contra la API de Preapproval (suscripciones recurrentes) de
/// Mercado Pago — mismo estilo que MercadoPagoGateway/MercadoPagoOAuthGateway: sin SDK, nunca deja
/// escapar una excepción de red/parseo.
/// </summary>
public class MercadoPagoRecurrenteGateway : IPagoRecurrenteGateway
{
    private const string BaseUrl = "https://api.mercadopago.com";

    private readonly HttpClient _httpClient;

    public MercadoPagoRecurrenteGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<PreapprovalCreadoResult>> CrearPreapprovalAsync(
        CrearPreapprovalRequest request, CancellationToken cancellationToken = default)
    {
        var (frequency, frequencyType) = MapearPeriodicidad(request.Periodicidad);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/preapproval");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);
        httpRequest.Content = JsonContent.Create(new
        {
            reason = request.Razon,
            external_reference = request.ExternalReferenceId.ToString(),
            payer_email = request.PayerEmail,
            back_url = request.BackUrl,
            notification_url = request.NotificationUrl,
            status = "pending",
            auto_recurring = new
            {
                frequency,
                frequency_type = frequencyType,
                transaction_amount = request.Monto,
                currency_id = "ARS"
            }
        });

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<PreapprovalCreadoResult>(
                    $"Mercado Pago rechazó la creación de la suscripción ({(int)response.StatusCode}).");

            var body = await response.Content.ReadFromJsonAsync<PreapprovalResponse>(cancellationToken);
            var initPoint = body?.InitPoint ?? body?.SandboxInitPoint;
            if (string.IsNullOrEmpty(body?.Id) || string.IsNullOrEmpty(initPoint))
                return Result.Failure<PreapprovalCreadoResult>("Respuesta inesperada de Mercado Pago al crear la suscripción.");

            return Result.Success(new PreapprovalCreadoResult(body.Id, initPoint));
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<PreapprovalCreadoResult>($"No se pudo contactar a Mercado Pago: {ex.Message}");
        }
    }

    public async Task<Result<PreapprovalEstadoResult>> ObtenerPreapprovalAsync(
        string accessToken, string preapprovalId, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/preapproval/{preapprovalId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<PreapprovalEstadoResult>(
                    $"Mercado Pago no devolvió la suscripción {preapprovalId} ({(int)response.StatusCode}).");

            var body = await response.Content.ReadFromJsonAsync<PreapprovalResponse>(cancellationToken);
            if (string.IsNullOrEmpty(body?.Id) || string.IsNullOrEmpty(body.Status))
                return Result.Failure<PreapprovalEstadoResult>("Respuesta inesperada de Mercado Pago al consultar la suscripción.");

            return Result.Success(new PreapprovalEstadoResult(body.Id, body.Status, body.ExternalReference));
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<PreapprovalEstadoResult>($"No se pudo contactar a Mercado Pago: {ex.Message}");
        }
    }

    public async Task<Result> CancelarPreapprovalAsync(
        string accessToken, string preapprovalId, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/preapproval/{preapprovalId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Content = JsonContent.Create(new { status = "cancelled" });

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure($"Mercado Pago no pudo cancelar la suscripción ({(int)response.StatusCode}).");

            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure($"No se pudo contactar a Mercado Pago: {ex.Message}");
        }
    }

    public async Task<Result> ActualizarMontoPreapprovalAsync(
        string accessToken, string preapprovalId, decimal nuevoMonto, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/preapproval/{preapprovalId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Content = JsonContent.Create(new { auto_recurring = new { transaction_amount = nuevoMonto } });

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure($"Mercado Pago no pudo actualizar el monto de la suscripción ({(int)response.StatusCode}).");

            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure($"No se pudo contactar a Mercado Pago: {ex.Message}");
        }
    }

    public async Task<Result<CargoRecurrenteResult>> ObtenerCargoRecurrenteAsync(
        string accessToken, string pagoExternoId, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/authorized_payments/{pagoExternoId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<CargoRecurrenteResult>(
                    $"Mercado Pago no devolvió el cargo {pagoExternoId} ({(int)response.StatusCode}).");

            var body = await response.Content.ReadFromJsonAsync<CargoRecurrenteResponse>(cancellationToken);
            if (string.IsNullOrEmpty(body?.Id) || string.IsNullOrEmpty(body.PreapprovalId) || body.Status is null)
                return Result.Failure<CargoRecurrenteResult>("Respuesta inesperada de Mercado Pago al consultar el cargo.");

            var estado = body.Status switch
            {
                "processed" or "approved" => EstadoPagoExterno.Aprobado,
                "rejected" or "cancelled" => EstadoPagoExterno.Rechazado,
                _ => EstadoPagoExterno.Pendiente
            };

            return Result.Success(new CargoRecurrenteResult(body.Id, body.PreapprovalId, body.TransactionAmount, estado));
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<CargoRecurrenteResult>($"No se pudo contactar a Mercado Pago: {ex.Message}");
        }
    }

    // Mercado Pago solo admite "days" o "months" como frequency_type: un plan Anual se modela como
    // 12 meses porque no existe un frequency_type de años.
    private static (int Frequency, string FrequencyType) MapearPeriodicidad(Periodicidad periodicidad) =>
        periodicidad == Periodicidad.Mensual ? (1, "months") : (12, "months");

    private record PreapprovalResponse(
        string? Id,
        string? Status,
        [property: JsonPropertyName("init_point")] string? InitPoint,
        [property: JsonPropertyName("sandbox_init_point")] string? SandboxInitPoint,
        [property: JsonPropertyName("external_reference")] string? ExternalReference);

    private record CargoRecurrenteResponse(
        string? Id,
        string? Status,
        [property: JsonPropertyName("preapproval_id")] string? PreapprovalId,
        [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount);
}
