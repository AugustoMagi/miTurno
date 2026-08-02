using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Application.Features.Suscripciones.Dtos;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/suscripcion")]
[Authorize(Roles = "Owner")]
public class SuscripcionController : ControllerBase
{
    private readonly ObtenerMiSuscripcionUseCase _obtenerMiSuscripcionUseCase;
    private readonly ElegirPlanUseCase _elegirPlanUseCase;
    private readonly IniciarSuscripcionMercadoPagoUseCase _iniciarSuscripcionMercadoPagoUseCase;
    private readonly CambiarPlanMiSuscripcionUseCase _cambiarPlanMiSuscripcionUseCase;
    private readonly CambiarPlanConPagoUseCase _cambiarPlanConPagoUseCase;
    private readonly CancelarMiSuscripcionUseCase _cancelarMiSuscripcionUseCase;
    private readonly ReanudarCobroAutomaticoUseCase _reanudarCobroAutomaticoUseCase;

    public SuscripcionController(
        ObtenerMiSuscripcionUseCase obtenerMiSuscripcionUseCase,
        ElegirPlanUseCase elegirPlanUseCase,
        IniciarSuscripcionMercadoPagoUseCase iniciarSuscripcionMercadoPagoUseCase,
        CambiarPlanMiSuscripcionUseCase cambiarPlanMiSuscripcionUseCase,
        CambiarPlanConPagoUseCase cambiarPlanConPagoUseCase,
        CancelarMiSuscripcionUseCase cancelarMiSuscripcionUseCase,
        ReanudarCobroAutomaticoUseCase reanudarCobroAutomaticoUseCase)
    {
        _obtenerMiSuscripcionUseCase = obtenerMiSuscripcionUseCase;
        _elegirPlanUseCase = elegirPlanUseCase;
        _iniciarSuscripcionMercadoPagoUseCase = iniciarSuscripcionMercadoPagoUseCase;
        _cambiarPlanMiSuscripcionUseCase = cambiarPlanMiSuscripcionUseCase;
        _cambiarPlanConPagoUseCase = cambiarPlanConPagoUseCase;
        _cancelarMiSuscripcionUseCase = cancelarMiSuscripcionUseCase;
        _reanudarCobroAutomaticoUseCase = reanudarCobroAutomaticoUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var result = await _obtenerMiSuscripcionUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost("elegir-plan")]
    public async Task<IActionResult> ElegirPlan(ElegirPlanRequest request, CancellationToken cancellationToken)
    {
        var result = await _elegirPlanUseCase.ExecuteAsync(User.GetNegocioId(), request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("suscribirme")]
    public async Task<IActionResult> Suscribirme([FromQuery] bool cobrarInmediato, CancellationToken cancellationToken)
    {
        var webhookBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _iniciarSuscripcionMercadoPagoUseCase.ExecuteAsync(
            User.GetNegocioId(), webhookBaseUrl, cobrarInmediato, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("plan")]
    public async Task<IActionResult> CambiarPlan(CambiarPlanMiSuscripcionRequest request, CancellationToken cancellationToken)
    {
        var result = await _cambiarPlanMiSuscripcionUseCase.ExecuteAsync(User.GetNegocioId(), request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("cambiar-plan-con-pago")]
    public async Task<IActionResult> CambiarPlanConPago(CambiarPlanMiSuscripcionRequest request, CancellationToken cancellationToken)
    {
        var webhookBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _cambiarPlanConPagoUseCase.ExecuteAsync(
            User.GetNegocioId(), request.NuevoPlanId, webhookBaseUrl, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("cancelar")]
    public async Task<IActionResult> Cancelar(CancellationToken cancellationToken)
    {
        var result = await _cancelarMiSuscripcionUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPatch("reanudar-cobro-automatico")]
    public async Task<IActionResult> ReanudarCobroAutomatico(CancellationToken cancellationToken)
    {
        var result = await _reanudarCobroAutomaticoUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
