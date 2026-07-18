using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/suscripcion")]
[Authorize(Roles = "Owner")]
public class SuscripcionController : ControllerBase
{
    private readonly ObtenerMiSuscripcionUseCase _obtenerMiSuscripcionUseCase;
    private readonly GenerarPagoSuscripcionUseCase _generarPagoSuscripcionUseCase;

    public SuscripcionController(
        ObtenerMiSuscripcionUseCase obtenerMiSuscripcionUseCase,
        GenerarPagoSuscripcionUseCase generarPagoSuscripcionUseCase)
    {
        _obtenerMiSuscripcionUseCase = obtenerMiSuscripcionUseCase;
        _generarPagoSuscripcionUseCase = generarPagoSuscripcionUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var result = await _obtenerMiSuscripcionUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost("pagar")]
    public async Task<IActionResult> Pagar(CancellationToken cancellationToken)
    {
        var webhookBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _generarPagoSuscripcionUseCase.ExecuteAsync(User.GetNegocioId(), webhookBaseUrl, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
