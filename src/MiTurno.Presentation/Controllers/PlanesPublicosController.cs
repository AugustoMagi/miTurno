using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Public;

namespace MiTurno.Presentation.Controllers;

/// <summary>Catálogo público de planes, para mostrar en la landing sin autenticación.</summary>
[ApiController]
[Route("api/public/planes")]
public class PlanesPublicosController : ControllerBase
{
    private readonly ListarPlanesPublicosUseCase _listarPlanesPublicosUseCase;

    public PlanesPublicosController(ListarPlanesPublicosUseCase listarPlanesPublicosUseCase)
    {
        _listarPlanesPublicosUseCase = listarPlanesPublicosUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var planes = await _listarPlanesPublicosUseCase.ExecuteAsync(cancellationToken);
        return Ok(planes);
    }
}
