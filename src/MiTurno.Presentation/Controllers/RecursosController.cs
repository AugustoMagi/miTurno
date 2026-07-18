using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Recursos;
using MiTurno.Application.Features.Recursos.Dtos;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/recursos")]
[Authorize(Roles = "Owner,Empleado")]
public class RecursosController : ControllerBase
{
    private readonly CrearRecursoUseCase _crearRecursoUseCase;
    private readonly ListarRecursosUseCase _listarRecursosUseCase;
    private readonly ObtenerRecursoUseCase _obtenerRecursoUseCase;
    private readonly ActualizarRecursoUseCase _actualizarRecursoUseCase;
    private readonly CambiarEstadoRecursoUseCase _cambiarEstadoRecursoUseCase;

    public RecursosController(
        CrearRecursoUseCase crearRecursoUseCase,
        ListarRecursosUseCase listarRecursosUseCase,
        ObtenerRecursoUseCase obtenerRecursoUseCase,
        ActualizarRecursoUseCase actualizarRecursoUseCase,
        CambiarEstadoRecursoUseCase cambiarEstadoRecursoUseCase)
    {
        _crearRecursoUseCase = crearRecursoUseCase;
        _listarRecursosUseCase = listarRecursosUseCase;
        _obtenerRecursoUseCase = obtenerRecursoUseCase;
        _actualizarRecursoUseCase = actualizarRecursoUseCase;
        _cambiarEstadoRecursoUseCase = cambiarEstadoRecursoUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearRecursoRequest request, CancellationToken cancellationToken)
    {
        var result = await _crearRecursoUseCase.ExecuteAsync(User.GetNegocioId(), request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var recursos = await _listarRecursosUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return Ok(recursos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var result = await _obtenerRecursoUseCase.ExecuteAsync(User.GetNegocioId(), id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, ActualizarRecursoRequest request, CancellationToken cancellationToken)
    {
        var result = await _actualizarRecursoUseCase.ExecuteAsync(User.GetNegocioId(), id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/activar")]
    public async Task<IActionResult> Activar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cambiarEstadoRecursoUseCase.ExecuteAsync(User.GetNegocioId(), id, activar: true, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/desactivar")]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cambiarEstadoRecursoUseCase.ExecuteAsync(User.GetNegocioId(), id, activar: false, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }
}
