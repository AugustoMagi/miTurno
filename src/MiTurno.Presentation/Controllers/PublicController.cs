using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Public;
using MiTurno.Application.Features.Public.Dtos;

namespace MiTurno.Presentation.Controllers;

/// <summary>Endpoints públicos consumidos desde el link de reserva del negocio (ej. su bio de Instagram), sin autenticación.</summary>
[ApiController]
[Route("api/public/negocios")]
public class PublicController : ControllerBase
{
    private readonly ObtenerNegocioPublicoUseCase _obtenerNegocioPublicoUseCase;
    private readonly ListarTurnosDisponiblesUseCase _listarTurnosDisponiblesUseCase;
    private readonly CrearReservaUseCase _crearReservaUseCase;
    private readonly CancelarReservaClienteUseCase _cancelarReservaClienteUseCase;
    private readonly ProcesarNotificacionPagoMercadoPagoUseCase _procesarNotificacionPagoMercadoPagoUseCase;

    public PublicController(
        ObtenerNegocioPublicoUseCase obtenerNegocioPublicoUseCase,
        ListarTurnosDisponiblesUseCase listarTurnosDisponiblesUseCase,
        CrearReservaUseCase crearReservaUseCase,
        CancelarReservaClienteUseCase cancelarReservaClienteUseCase,
        ProcesarNotificacionPagoMercadoPagoUseCase procesarNotificacionPagoMercadoPagoUseCase)
    {
        _obtenerNegocioPublicoUseCase = obtenerNegocioPublicoUseCase;
        _listarTurnosDisponiblesUseCase = listarTurnosDisponiblesUseCase;
        _crearReservaUseCase = crearReservaUseCase;
        _cancelarReservaClienteUseCase = cancelarReservaClienteUseCase;
        _procesarNotificacionPagoMercadoPagoUseCase = procesarNotificacionPagoMercadoPagoUseCase;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> ObtenerNegocio(string slug, CancellationToken cancellationToken)
    {
        var result = await _obtenerNegocioPublicoUseCase.ExecuteAsync(slug, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpGet("{slug}/recursos/{recursoId:guid}/turnos")]
    public async Task<IActionResult> ListarTurnosDisponibles(
        string slug, Guid recursoId, [FromQuery] DateOnly fecha, CancellationToken cancellationToken)
    {
        var result = await _listarTurnosDisponiblesUseCase.ExecuteAsync(slug, recursoId, fecha, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{slug}/recursos/{recursoId:guid}/reservas")]
    public async Task<IActionResult> CrearReserva(
        string slug, Guid recursoId, CrearReservaRequest request, CancellationToken cancellationToken)
    {
        var webhookBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _crearReservaUseCase.ExecuteAsync(slug, recursoId, request, webhookBaseUrl, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{slug}/reservas/{reservaId:guid}/cancelar")]
    public async Task<IActionResult> CancelarReserva(string slug, Guid reservaId, CancellationToken cancellationToken)
    {
        var result = await _cancelarReservaClienteUseCase.ExecuteAsync(slug, reservaId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Webhook de Mercado Pago para el pago de una Reserva puntual (la URL ya trae el reservaId
    /// porque se generó al crear la preferencia de esa reserva). Mercado Pago manda el id del pago
    /// como "data.id" (webhooks nuevos, type=payment) o como "id" (IPN viejo, topic=payment); se
    /// acepta cualquiera de los dos formatos.
    /// A diferencia del resto del controller, acá el código de respuesta no es solo informativo:
    /// Mercado Pago reintenta la notificación si no recibe 200/201, así que devolvemos 500 ante
    /// errores transitorios (para que reintente) en vez del BadRequest habitual.
    /// </summary>
    [HttpPost("{slug}/reservas/{reservaId:guid}/pago/webhook/mercadopago")]
    public async Task<IActionResult> WebhookMercadoPago(
        string slug, Guid reservaId,
        [FromQuery] string? type, [FromQuery] string? topic,
        [FromQuery(Name = "data.id")] string? dataId, [FromQuery] string? id,
        CancellationToken cancellationToken)
    {
        var esNotificacionDePago = string.Equals(type ?? topic, "payment", StringComparison.OrdinalIgnoreCase);
        if (!esNotificacionDePago)
            return Ok();

        var result = await _procesarNotificacionPagoMercadoPagoUseCase.ExecuteAsync(
            slug, reservaId, dataId ?? id, cancellationToken);
        return result.IsSuccess ? Ok() : StatusCode(StatusCodes.Status500InternalServerError, new { error = result.Error });
    }
}
