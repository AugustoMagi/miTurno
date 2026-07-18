using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Recursos.Bloqueos;
using MiTurno.Application.Features.Recursos.Bloqueos.Dtos;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/recursos/{recursoId:guid}/bloqueos")]
[Authorize(Roles = "Owner,Empleado")]
public class BloqueosFechaController : ControllerBase
{
    private readonly AgregarBloqueoFechaUseCase _agregarBloqueoFechaUseCase;
    private readonly ListarBloqueosFechaUseCase _listarBloqueosFechaUseCase;
    private readonly EliminarBloqueoFechaUseCase _eliminarBloqueoFechaUseCase;

    public BloqueosFechaController(
        AgregarBloqueoFechaUseCase agregarBloqueoFechaUseCase,
        ListarBloqueosFechaUseCase listarBloqueosFechaUseCase,
        EliminarBloqueoFechaUseCase eliminarBloqueoFechaUseCase)
    {
        _agregarBloqueoFechaUseCase = agregarBloqueoFechaUseCase;
        _listarBloqueosFechaUseCase = listarBloqueosFechaUseCase;
        _eliminarBloqueoFechaUseCase = eliminarBloqueoFechaUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> Agregar(Guid recursoId, AgregarBloqueoFechaRequest request, CancellationToken cancellationToken)
    {
        var result = await _agregarBloqueoFechaUseCase.ExecuteAsync(User.GetNegocioId(), recursoId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> Listar(Guid recursoId, CancellationToken cancellationToken)
    {
        var result = await _listarBloqueosFechaUseCase.ExecuteAsync(User.GetNegocioId(), recursoId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpDelete("{bloqueoId:guid}")]
    public async Task<IActionResult> Eliminar(Guid recursoId, Guid bloqueoId, CancellationToken cancellationToken)
    {
        var result = await _eliminarBloqueoFechaUseCase.ExecuteAsync(User.GetNegocioId(), recursoId, bloqueoId, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
