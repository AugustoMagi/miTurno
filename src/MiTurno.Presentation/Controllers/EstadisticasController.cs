using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Estadisticas;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/estadisticas")]
[Authorize(Roles = "Owner,Empleado")]
public class EstadisticasController : ControllerBase
{
    private readonly ObtenerEstadisticasOcupacionUseCase _obtenerEstadisticasOcupacionUseCase;

    public EstadisticasController(ObtenerEstadisticasOcupacionUseCase obtenerEstadisticasOcupacionUseCase)
    {
        _obtenerEstadisticasOcupacionUseCase = obtenerEstadisticasOcupacionUseCase;
    }

    [HttpGet("ocupacion")]
    public async Task<IActionResult> ObtenerOcupacion(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken cancellationToken)
    {
        var estadisticas = await _obtenerEstadisticasOcupacionUseCase.ExecuteAsync(
            User.GetNegocioId(), desde, hasta, cancellationToken);
        return Ok(estadisticas);
    }
}
