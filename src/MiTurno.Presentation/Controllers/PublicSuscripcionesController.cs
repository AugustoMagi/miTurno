using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Suscripciones;

namespace MiTurno.Presentation.Controllers;

/// <summary>Webhook público de Mercado Pago para el cobro recurrente de una Suscripcion (cobro de MiTurno al negocio).</summary>
[ApiController]
[Route("api/public/suscripciones")]
public class PublicSuscripcionesController : ControllerBase
{
    private readonly ProcesarNotificacionRecurrenteUseCase _procesarNotificacionRecurrenteUseCase;

    public PublicSuscripcionesController(ProcesarNotificacionRecurrenteUseCase procesarNotificacionRecurrenteUseCase)
    {
        _procesarNotificacionRecurrenteUseCase = procesarNotificacionRecurrenteUseCase;
    }

    /// <summary>
    /// Mercado Pago manda "type=subscription_authorized_payment" en cada cargo periódico y
    /// "type=subscription_preapproval" cuando cambia el estado de la propia autorización, con el id
    /// del recurso afectado en "data.id" (o "id" en el formato viejo). Mismo criterio que el resto de
    /// los webhooks: 500 ante errores transitorios (para que reintente), 200 en cualquier otro caso.
    /// </summary>
    [HttpPost("{suscripcionId:guid}/webhook/recurrente")]
    public async Task<IActionResult> WebhookRecurrente(
        Guid suscripcionId,
        [FromQuery] string? type, [FromQuery] string? topic,
        [FromQuery(Name = "data.id")] string? dataId, [FromQuery] string? id,
        CancellationToken cancellationToken)
    {
        var tipo = type ?? topic;
        if (string.IsNullOrWhiteSpace(tipo))
            return Ok();

        var result = await _procesarNotificacionRecurrenteUseCase.ExecuteAsync(
            suscripcionId, tipo, dataId ?? id, cancellationToken);
        return result.IsSuccess ? Ok() : StatusCode(StatusCodes.Status500InternalServerError, new { error = result.Error });
    }
}
