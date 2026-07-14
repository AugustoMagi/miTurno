using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Public;

namespace MiTurno.Presentation.Controllers;

/// <summary>Endpoints públicos consumidos desde el link de reserva del negocio (ej. su bio de Instagram), sin autenticación.</summary>
[ApiController]
[Route("api/public/negocios")]
public class PublicController : ControllerBase
{
    private readonly ObtenerNegocioPublicoUseCase _obtenerNegocioPublicoUseCase;
    private readonly ListarTurnosDisponiblesUseCase _listarTurnosDisponiblesUseCase;

    public PublicController(
        ObtenerNegocioPublicoUseCase obtenerNegocioPublicoUseCase,
        ListarTurnosDisponiblesUseCase listarTurnosDisponiblesUseCase)
    {
        _obtenerNegocioPublicoUseCase = obtenerNegocioPublicoUseCase;
        _listarTurnosDisponiblesUseCase = listarTurnosDisponiblesUseCase;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> ObtenerNegocio(string slug, CancellationToken cancellationToken)
    {
        var result = await _obtenerNegocioPublicoUseCase.ExecuteAsync(slug, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpGet("{slug}/recursos/{recursoId:guid}/turnos")]
    public async Task<IActionResult> ListarTurnosDisponibles(
        string slug, Guid recursoId, [FromQuery] DateOnly fecha, CancellationToken cancellationToken)
    {
        var result = await _listarTurnosDisponiblesUseCase.ExecuteAsync(slug, recursoId, fecha, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
