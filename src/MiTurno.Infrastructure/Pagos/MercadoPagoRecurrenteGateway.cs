using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
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

    // Mensaje genérico para cualquier rechazo o falla de red hablando con Mercado Pago: el detalle
    // real (código, body de error, excepción) sólo se loguea acá adentro — nunca sale de este
    // gateway hacia el Result.Failure, que el negocio termina viendo tal cual en el frontend.
    private const string ErrorGenerico = "Ocurrió un error al comunicarnos con Mercado Pago. Probá de nuevo en unos minutos.";

    private readonly HttpClient _httpClient;
    private readonly ILogger<MercadoPagoRecurrenteGateway> _logger;

    public MercadoPagoRecurrenteGateway(HttpClient httpClient, ILogger<MercadoPagoRecurrenteGateway> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
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
                await LoguearErrorAsync(response, "crear la Preapproval", cancellationToken);
                return Result.Failure<PreapprovalCreadoResult>(ErrorGenerico);
            }

            var body = await response.Content.ReadFromJsonAsync<PreapprovalResponse>(cancellationToken);
            var initPoint = body?.InitPoint ?? body?.SandboxInitPoint;
            if (string.IsNullOrEmpty(body?.Id) || string.IsNullOrEmpty(initPoint))
            {
                _logger.LogWarning("Mercado Pago devolvió una respuesta inesperada al crear la Preapproval: {Body}", body);
                return Result.Failure<PreapprovalCreadoResult>(ErrorGenerico);
            }

            return Result.Success(new PreapprovalCreadoResult(body.Id, initPoint));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo contactar a Mercado Pago para crear la Preapproval.");
            return Result.Failure<PreapprovalCreadoResult>(ErrorGenerico);
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
            {
                await LoguearErrorAsync(response, $"consultar la Preapproval {preapprovalId}", cancellationToken);
                return Result.Failure<PreapprovalEstadoResult>(ErrorGenerico);
            }

            var body = await response.Content.ReadFromJsonAsync<PreapprovalResponse>(cancellationToken);
            if (string.IsNullOrEmpty(body?.Id) || string.IsNullOrEmpty(body.Status))
            {
                _logger.LogWarning("Mercado Pago devolvió una respuesta inesperada al consultar la Preapproval {PreapprovalId}: {Body}", preapprovalId, body);
                return Result.Failure<PreapprovalEstadoResult>(ErrorGenerico);
            }

            return Result.Success(new PreapprovalEstadoResult(body.Id, body.Status, body.ExternalReference));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo contactar a Mercado Pago para consultar la Preapproval {PreapprovalId}.", preapprovalId);
            return Result.Failure<PreapprovalEstadoResult>(ErrorGenerico);
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
                await LoguearErrorAsync(response, $"{accion} la Preapproval {preapprovalId}", cancellationToken);
                return Result.Failure(ErrorGenerico);
            }

            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo contactar a Mercado Pago para {Accion} la Preapproval {PreapprovalId}.", accion, preapprovalId);
            return Result.Failure(ErrorGenerico);
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
                await LoguearErrorAsync(response, $"actualizar el monto de la Preapproval {preapprovalId}", cancellationToken);
                return Result.Failure(ErrorGenerico);
            }

            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo contactar a Mercado Pago para actualizar el monto de la Preapproval {PreapprovalId}.", preapprovalId);
            return Result.Failure(ErrorGenerico);
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
            {
                await LoguearErrorAsync(response, $"consultar el cargo {pagoExternoId}", cancellationToken);
                return Result.Failure<CargoRecurrenteResult>(ErrorGenerico);
            }

            var body = await response.Content.ReadFromJsonAsync<CargoRecurrenteResponse>(cancellationToken);
            if (body?.Id is null || string.IsNullOrEmpty(body.PreapprovalId) || body.Status is null)
            {
                _logger.LogWarning("Mercado Pago devolvió una respuesta inesperada al consultar el cargo {PagoExternoId}: {Body}", pagoExternoId, body);
                return Result.Failure<CargoRecurrenteResult>(ErrorGenerico);
            }

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
            _logger.LogError(ex, "No se pudo contactar a Mercado Pago para consultar el cargo {PagoExternoId}.", pagoExternoId);
            return Result.Failure<CargoRecurrenteResult>(ErrorGenerico);
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
            {
                await LoguearErrorAsync(response, $"consultar los cargos de la Preapproval {preapprovalId}", cancellationToken);
                return Result.Failure<string?>(ErrorGenerico);
            }

            var body = await response.Content.ReadFromJsonAsync<AuthorizedPaymentsSearchResponse>(cancellationToken);
            var ultimoProcesado = body?.Results?
                .Where(r => r.Status is "processed" or "approved")
                .OrderByDescending(r => r.DateCreated)
                .FirstOrDefault();

            return Result.Success(ultimoProcesado?.Id?.ToString());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo contactar a Mercado Pago para consultar los cargos de la Preapproval {PreapprovalId}.", preapprovalId);
            return Result.Failure<string?>(ErrorGenerico);
        }
    }

    // Mercado Pago solo admite "days" o "months" como frequency_type: un plan Anual se modela como
    // 12 meses porque no existe un frequency_type de años.
    private static (int Frequency, string FrequencyType) MapearPeriodicidad(Periodicidad periodicidad) =>
        periodicidad == Periodicidad.Mensual ? (1, "months") : (12, "months");

    // El detalle real de un rechazo de Mercado Pago (código + body de error, que suele traer un
    // campo "message" bien puntual) sólo se loguea acá — nunca cruza hacia el Result.Failure que
    // termina en el frontend, para no exponerle al negocio un error técnico en su propio idioma.
    private async Task LoguearErrorAsync(HttpResponseMessage response, string accion, CancellationToken cancellationToken)
    {
        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            body = string.Empty;
        }

        _logger.LogWarning(
            "Mercado Pago rechazó el intento de {Accion} ({StatusCode}): {Body}",
            accion, (int)response.StatusCode, body);
    }

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
