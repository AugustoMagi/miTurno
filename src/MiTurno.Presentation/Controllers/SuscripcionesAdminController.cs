using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Admin.Suscripciones;
using MiTurno.Application.Features.Admin.Suscripciones.Dtos;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/admin/suscripciones")]
[Authorize(Roles = "SysAdmin")]
public class SuscripcionesAdminController : ControllerBase
{
    private readonly ListarSuscripcionesUseCase _listarSuscripcionesUseCase;
    private readonly CambiarPlanSuscripcionUseCase _cambiarPlanSuscripcionUseCase;
    private readonly RenovarSuscripcionManualUseCase _renovarSuscripcionManualUseCase;
    private readonly CancelarSuscripcionUseCase _cancelarSuscripcionUseCase;

    public SuscripcionesAdminController(
        ListarSuscripcionesUseCase listarSuscripcionesUseCase,
        CambiarPlanSuscripcionUseCase cambiarPlanSuscripcionUseCase,
        RenovarSuscripcionManualUseCase renovarSuscripcionManualUseCase,
        CancelarSuscripcionUseCase cancelarSuscripcionUseCase)
    {
        _listarSuscripcionesUseCase = listarSuscripcionesUseCase;
        _cambiarPlanSuscripcionUseCase = cambiarPlanSuscripcionUseCase;
        _renovarSuscripcionManualUseCase = renovarSuscripcionManualUseCase;
        _cancelarSuscripcionUseCase = cancelarSuscripcionUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var suscripciones = await _listarSuscripcionesUseCase.ExecuteAsync(cancellationToken);
        return Ok(suscripciones);
    }

    [HttpPatch("{id:guid}/plan")]
    public async Task<IActionResult> CambiarPlan(Guid id, CambiarPlanSuscripcionRequest request, CancellationToken cancellationToken)
    {
        var result = await _cambiarPlanSuscripcionUseCase.ExecuteAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/renovar")]
    public async Task<IActionResult> Renovar(Guid id, RenovarSuscripcionManualRequest request, CancellationToken cancellationToken)
    {
        var result = await _renovarSuscripcionManualUseCase.ExecuteAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cancelarSuscripcionUseCase.ExecuteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
