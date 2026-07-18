using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.ConfiguracionesPago;
using MiTurno.Application.Features.ConfiguracionesPago.Dtos;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/configuracion-pago")]
[Authorize(Roles = "Owner,Empleado")]
public class ConfiguracionesPagoController : ControllerBase
{
    private readonly ConectarConfiguracionPagoUseCase _conectarConfiguracionPagoUseCase;
    private readonly ObtenerConfiguracionPagoUseCase _obtenerConfiguracionPagoUseCase;
    private readonly DesconectarConfiguracionPagoUseCase _desconectarConfiguracionPagoUseCase;

    public ConfiguracionesPagoController(
        ConectarConfiguracionPagoUseCase conectarConfiguracionPagoUseCase,
        ObtenerConfiguracionPagoUseCase obtenerConfiguracionPagoUseCase,
        DesconectarConfiguracionPagoUseCase desconectarConfiguracionPagoUseCase)
    {
        _conectarConfiguracionPagoUseCase = conectarConfiguracionPagoUseCase;
        _obtenerConfiguracionPagoUseCase = obtenerConfiguracionPagoUseCase;
        _desconectarConfiguracionPagoUseCase = desconectarConfiguracionPagoUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> Conectar(ConectarConfiguracionPagoRequest request, CancellationToken cancellationToken)
    {
        var result = await _conectarConfiguracionPagoUseCase.ExecuteAsync(User.GetNegocioId(), request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var result = await _obtenerConfiguracionPagoUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpDelete]
    public async Task<IActionResult> Desconectar(CancellationToken cancellationToken)
    {
        var result = await _desconectarConfiguracionPagoUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
