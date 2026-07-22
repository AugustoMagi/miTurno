using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Negocios;
using MiTurno.Application.Features.Negocios.Dtos;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

/// <summary>Datos propios del negocio del usuario autenticado (nombre, link público, contacto), no del Usuario (ver PerfilController).</summary>
[ApiController]
[Route("api/mi-negocio")]
[Authorize(Roles = "Owner,Empleado")]
public class NegocioController : ControllerBase
{
    private readonly ObtenerMiNegocioUseCase _obtenerMiNegocioUseCase;
    private readonly ActualizarMiNegocioUseCase _actualizarMiNegocioUseCase;

    public NegocioController(
        ObtenerMiNegocioUseCase obtenerMiNegocioUseCase, ActualizarMiNegocioUseCase actualizarMiNegocioUseCase)
    {
        _obtenerMiNegocioUseCase = obtenerMiNegocioUseCase;
        _actualizarMiNegocioUseCase = actualizarMiNegocioUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var result = await _obtenerMiNegocioUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar(ActualizarMiNegocioRequest request, CancellationToken cancellationToken)
    {
        var result = await _actualizarMiNegocioUseCase.ExecuteAsync(User.GetNegocioId(), request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
