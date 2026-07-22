using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Admin.Facturacion;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/admin/facturacion")]
[Authorize(Roles = "SysAdmin")]
public class FacturacionAdminController : ControllerBase
{
    private readonly ObtenerFacturacionPlataformaUseCase _obtenerFacturacionPlataformaUseCase;

    public FacturacionAdminController(ObtenerFacturacionPlataformaUseCase obtenerFacturacionPlataformaUseCase)
    {
        _obtenerFacturacionPlataformaUseCase = obtenerFacturacionPlataformaUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken cancellationToken)
    {
        var facturacion = await _obtenerFacturacionPlataformaUseCase.ExecuteAsync(desde, hasta, cancellationToken);
        return Ok(facturacion);
    }
}
