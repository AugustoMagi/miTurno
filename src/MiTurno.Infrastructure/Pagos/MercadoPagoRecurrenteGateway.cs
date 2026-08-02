using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

        // Sin FechaInicio, no se manda start_date en absoluto: dejar que Mercado Pago cobre "ahora"
        // por su cuenta es más seguro que calcular un "ahora" acá y mandarlo, porque la latencia de
        // red puede hacer que ya esté en el pasado cuando MP lo valida (rechaza con "cannot be a past
        // date"). Sólo se manda cuando hay una fecha futura real a la que diferir el primer cobro.
        var autoRecurring = new Dictionary<string, object>
        {
            ["frequency"] = frequency,
            ["frequency_type"] = frequencyType,
            ["transaction_amount"] = request.Monto,
            ["currency_id"] = "ARS"
        };
        if (request.FechaInicio is { } fechaInicio)
            // Offset explícito (+00:00) en vez de "Z": es el formato que usan los ejemplos de la
            // documentación de Mercado Pago para start_date/end_date.
            autoRecurring["start_date"] = new DateTimeOffset(fechaInicio, TimeSpan.Zero).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

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
            auto_recurring = autoRecurring
        });

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detalle = await LeerMensajeDeErrorAsync(response, cancellationToken);
                return Result.Failure<PreapprovalCreadoResult>(
                    $"Mercado Pago rechazó la creación de la suscripción ({(int)response.StatusCode}){detalle}.");
            }

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

    public Task<Result> CancelarPreapprovalAsync(
        string accessToken, string preapprovalId, CancellationToken cancellationToken = default) =>
        ActualizarStatusAsync(accessToken, preapprovalId, "cancelled", "cancelar", cancellationToken);

    public Task<Result> PausarPreapprovalAsync(
        string accessToken, string preapprovalId, CancellationToken cancellationToken = default) =>
        ActualizarStatusAsync(accessToken, preapprovalId, "paused", "pausar", cancellationToken);

    public Task<Result> ReanudarPreapprovalAsync(
        string accessToken, string preapprovalId, CancellationToken cancellationToken = default) =>
        ActualizarStatusAsync(accessToken, preapprovalId, "authorized", "reanudar", cancellationToken);

    private async Task<Result> ActualizarStatusAsync(
        string accessToken, string preapprovalId, string status, string accion, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/preapproval/{preapprovalId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Content = JsonContent.Create(new { status });

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detalle = await LeerMensajeDeErrorAsync(response, cancellationToken);
                return Result.Failure($"Mercado Pago no pudo {accion} la suscripción ({(int)response.StatusCode}){detalle}.");
            }

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
            {
                var detalle = await LeerMensajeDeErrorAsync(response, cancellationToken);
                return Result.Failure($"Mercado Pago no pudo actualizar el monto de la suscripción ({(int)response.StatusCode}){detalle}.");
            }

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
            if (body?.Id is null || string.IsNullOrEmpty(body.PreapprovalId) || body.Status is null)
                return Result.Failure<CargoRecurrenteResult>("Respuesta inesperada de Mercado Pago al consultar el cargo.");

            var estado = body.Status switch
            {
                "processed" or "approved" => EstadoPagoExterno.Aprobado,
                "rejected" or "cancelled" => EstadoPagoExterno.Rechazado,
                _ => EstadoPagoExterno.Pendiente
            };

            return Result.Success(new CargoRecurrenteResult(body.Id.Value.ToString(), body.PreapprovalId, body.TransactionAmount, estado));
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<CargoRecurrenteResult>($"No se pudo contactar a Mercado Pago: {ex.Message}");
        }
    }

    public async Task<Result<string?>> BuscarUltimoCargoIdAsync(
        string accessToken, string preapprovalId, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get, $"{BaseUrl}/authorized_payments/search?preapproval_id={preapprovalId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<string?>(
                    $"Mercado Pago no devolvió los cargos de la suscripción {preapprovalId} ({(int)response.StatusCode}).");

            var body = await response.Content.ReadFromJsonAsync<AuthorizedPaymentsSearchResponse>(cancellationToken);
            var ultimoProcesado = body?.Results?
                .Where(r => r.Status is "processed" or "approved")
                .OrderByDescending(r => r.DateCreated)
                .FirstOrDefault();

            return Result.Success(ultimoProcesado?.Id?.ToString());
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<string?>($"No se pudo contactar a Mercado Pago: {ex.Message}");
        }
    }

    // Mercado Pago solo admite "days" o "months" como frequency_type: un plan Anual se modela como
    // 12 meses porque no existe un frequency_type de años.
    private static (int Frequency, string FrequencyType) MapearPeriodicidad(Periodicidad periodicidad) =>
        periodicidad == Periodicidad.Mensual ? (1, "months") : (12, "months");

    // Sin esto, un rechazo de Mercado Pago sólo mostraba el código HTTP (ej. "(400)"), sin decir qué
    // campo estaba mal — imposible de diagnosticar a distancia. El body de error de MP normalmente
    // trae un campo "message" con el motivo puntual; si no se puede parsear, se manda el body crudo
    // (acotado) en vez de nada.
    private static async Task<string> LeerMensajeDeErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        try
        {
            var error = JsonSerializer.Deserialize<MercadoPagoErrorResponse>(body);
            if (!string.IsNullOrWhiteSpace(error?.Message))
                return $": {error.Message}";
        }
        catch (JsonException)
        {
            // el body no era el JSON de error esperado, se cae al texto crudo de abajo
        }

        return $": {(body.Length > 300 ? body[..300] : body)}";
    }

    private record MercadoPagoErrorResponse(string? Message);

    private record PreapprovalResponse(
        string? Id,
        string? Status,
        [property: JsonPropertyName("init_point")] string? InitPoint,
        [property: JsonPropertyName("sandbox_init_point")] string? SandboxInitPoint,
        [property: JsonPropertyName("external_reference")] string? ExternalReference);

    // Mercado Pago devuelve el "id" de un authorized_payment como número JSON (no como string, a
    // diferencia del id de la Preapproval que sí es un string hex) — de ahí el long? acá.
    private record CargoRecurrenteResponse(
        long? Id,
        string? Status,
        [property: JsonPropertyName("preapproval_id")] string? PreapprovalId,
        [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount);

    private record AuthorizedPaymentsSearchResponse(List<AuthorizedPaymentItem>? Results);

    private record AuthorizedPaymentItem(
        long? Id, string? Status, [property: JsonPropertyName("date_created")] DateTimeOffset? DateCreated);
}
