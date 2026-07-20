using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Perfil;
using MiTurno.Application.Features.Perfil.Dtos;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

/// <summary>Datos propios del usuario autenticado (dueño o empleado), no de su negocio.</summary>
[ApiController]
[Route("api/perfil")]
[Authorize]
public class PerfilController : ControllerBase
{
    private readonly ObtenerMiPerfilUseCase _obtenerMiPerfilUseCase;
    private readonly ActualizarMiPerfilUseCase _actualizarMiPerfilUseCase;
    private readonly CambiarMiPasswordUseCase _cambiarMiPasswordUseCase;

    public PerfilController(
        ObtenerMiPerfilUseCase obtenerMiPerfilUseCase,
        ActualizarMiPerfilUseCase actualizarMiPerfilUseCase,
        CambiarMiPasswordUseCase cambiarMiPasswordUseCase)
    {
        _obtenerMiPerfilUseCase = obtenerMiPerfilUseCase;
        _actualizarMiPerfilUseCase = actualizarMiPerfilUseCase;
        _cambiarMiPasswordUseCase = cambiarMiPasswordUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var result = await _obtenerMiPerfilUseCase.ExecuteAsync(User.GetUsuarioId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar(ActualizarMiPerfilRequest request, CancellationToken cancellationToken)
    {
        var result = await _actualizarMiPerfilUseCase.ExecuteAsync(User.GetUsuarioId(), request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("password")]
    public async Task<IActionResult> CambiarPassword(CambiarMiPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _cambiarMiPasswordUseCase.ExecuteAsync(User.GetUsuarioId(), request, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
