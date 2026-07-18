using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Suscripciones;

namespace MiTurno.Presentation.Controllers;

/// <summary>Webhook público de Mercado Pago para el pago de una Suscripcion (cobro de MiTurno al negocio).</summary>
[ApiController]
[Route("api/public/suscripciones")]
public class PublicSuscripcionesController : ControllerBase
{
    private readonly ProcesarNotificacionPagoSuscripcionUseCase _procesarNotificacionPagoSuscripcionUseCase;

    public PublicSuscripcionesController(
        ProcesarNotificacionPagoSuscripcionUseCase procesarNotificacionPagoSuscripcionUseCase)
    {
        _procesarNotificacionPagoSuscripcionUseCase = procesarNotificacionPagoSuscripcionUseCase;
    }

    /// <summary>
    /// Igual criterio que el webhook de reservas: Mercado Pago manda el id del pago como "data.id"
    /// (type=payment) o "id" (topic=payment), y el código de respuesta controla si reintenta
    /// (500 ante errores transitorios) o no (200 en cualquier otro caso).
    /// </summary>
    [HttpPost("{suscripcionId:guid}/pago/webhook/mercadopago")]
    public async Task<IActionResult> WebhookMercadoPago(
        Guid suscripcionId,
        [FromQuery] string? type, [FromQuery] string? topic,
        [FromQuery(Name = "data.id")] string? dataId, [FromQuery] string? id,
        CancellationToken cancellationToken)
    {
        var esNotificacionDePago = string.Equals(type ?? topic, "payment", StringComparison.OrdinalIgnoreCase);
        if (!esNotificacionDePago)
            return Ok();

        var result = await _procesarNotificacionPagoSuscripcionUseCase.ExecuteAsync(
            suscripcionId, dataId ?? id, cancellationToken);
        return result.IsSuccess ? Ok() : StatusCode(StatusCodes.Status500InternalServerError, new { error = result.Error });
    }
}
