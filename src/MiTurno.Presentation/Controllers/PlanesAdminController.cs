using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Admin.Planes;
using MiTurno.Application.Features.Admin.Planes.Dtos;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/admin/planes")]
[Authorize(Roles = "SysAdmin")]
public class PlanesAdminController : ControllerBase
{
    private readonly CrearPlanUseCase _crearPlanUseCase;
    private readonly ActualizarPlanUseCase _actualizarPlanUseCase;
    private readonly ListarPlanesUseCase _listarPlanesUseCase;
    private readonly DesactivarPlanUseCase _desactivarPlanUseCase;
    private readonly EliminarPlanUseCase _eliminarPlanUseCase;
    private readonly MarcarPlanDePruebaUseCase _marcarPlanDePruebaUseCase;
    private readonly DesmarcarPlanDePruebaUseCase _desmarcarPlanDePruebaUseCase;

    public PlanesAdminController(
        CrearPlanUseCase crearPlanUseCase,
        ActualizarPlanUseCase actualizarPlanUseCase,
        ListarPlanesUseCase listarPlanesUseCase,
        DesactivarPlanUseCase desactivarPlanUseCase,
        EliminarPlanUseCase eliminarPlanUseCase,
        MarcarPlanDePruebaUseCase marcarPlanDePruebaUseCase,
        DesmarcarPlanDePruebaUseCase desmarcarPlanDePruebaUseCase)
    {
        _crearPlanUseCase = crearPlanUseCase;
        _actualizarPlanUseCase = actualizarPlanUseCase;
        _listarPlanesUseCase = listarPlanesUseCase;
        _desactivarPlanUseCase = desactivarPlanUseCase;
        _eliminarPlanUseCase = eliminarPlanUseCase;
        _marcarPlanDePruebaUseCase = marcarPlanDePruebaUseCase;
        _desmarcarPlanDePruebaUseCase = desmarcarPlanDePruebaUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var planes = await _listarPlanesUseCase.ExecuteAsync(cancellationToken);
        return Ok(planes);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearPlanRequest request, CancellationToken cancellationToken)
    {
        var result = await _crearPlanUseCase.ExecuteAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, ActualizarPlanRequest request, CancellationToken cancellationToken)
    {
        var result = await _actualizarPlanUseCase.ExecuteAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/desactivar")]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _desactivarPlanUseCase.ExecuteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/marcar-de-prueba")]
    public async Task<IActionResult> MarcarDePrueba(Guid id, CancellationToken cancellationToken)
    {
        var result = await _marcarPlanDePruebaUseCase.ExecuteAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/desmarcar-de-prueba")]
    public async Task<IActionResult> DesmarcarDePrueba(Guid id, CancellationToken cancellationToken)
    {
        var result = await _desmarcarPlanDePruebaUseCase.ExecuteAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _eliminarPlanUseCase.ExecuteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
