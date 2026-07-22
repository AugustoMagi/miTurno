using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Admin.Negocios;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/admin/negocios")]
[Authorize(Roles = "SysAdmin")]
public class NegociosAdminController : ControllerBase
{
    private readonly ListarNegociosUseCase _listarNegociosUseCase;
    private readonly ObtenerNegocioDetalleUseCase _obtenerNegocioDetalleUseCase;
    private readonly CambiarEstadoNegocioUseCase _cambiarEstadoNegocioUseCase;

    public NegociosAdminController(
        ListarNegociosUseCase listarNegociosUseCase,
        ObtenerNegocioDetalleUseCase obtenerNegocioDetalleUseCase,
        CambiarEstadoNegocioUseCase cambiarEstadoNegocioUseCase)
    {
        _listarNegociosUseCase = listarNegociosUseCase;
        _obtenerNegocioDetalleUseCase = obtenerNegocioDetalleUseCase;
        _cambiarEstadoNegocioUseCase = cambiarEstadoNegocioUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var negocios = await _listarNegociosUseCase.ExecuteAsync(cancellationToken);
        return Ok(negocios);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerDetalle(Guid id, CancellationToken cancellationToken)
    {
        var result = await _obtenerNegocioDetalleUseCase.ExecuteAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/activar")]
    public async Task<IActionResult> Activar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cambiarEstadoNegocioUseCase.ExecuteAsync(id, activar: true, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/desactivar")]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cambiarEstadoNegocioUseCase.ExecuteAsync(id, activar: false, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
