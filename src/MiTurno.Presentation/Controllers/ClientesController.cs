using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Clientes;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Roles = "Owner,Empleado")]
public class ClientesController : ControllerBase
{
    private readonly ListarClientesUseCase _listarClientesUseCase;
    private readonly ObtenerHistorialClienteUseCase _obtenerHistorialClienteUseCase;

    public ClientesController(
        ListarClientesUseCase listarClientesUseCase,
        ObtenerHistorialClienteUseCase obtenerHistorialClienteUseCase)
    {
        _listarClientesUseCase = listarClientesUseCase;
        _obtenerHistorialClienteUseCase = obtenerHistorialClienteUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var clientes = await _listarClientesUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return Ok(clientes);
    }

    [HttpGet("{clienteId:guid}")]
    public async Task<IActionResult> ObtenerHistorial(Guid clienteId, CancellationToken cancellationToken)
    {
        var result = await _obtenerHistorialClienteUseCase.ExecuteAsync(User.GetNegocioId(), clienteId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}
