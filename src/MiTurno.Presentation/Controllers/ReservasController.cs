using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Reservas;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/reservas")]
[Authorize]
public class ReservasController : ControllerBase
{
    private readonly ListarReservasUseCase _listarReservasUseCase;
    private readonly CancelarReservaUseCase _cancelarReservaUseCase;

    public ReservasController(
        ListarReservasUseCase listarReservasUseCase,
        CancelarReservaUseCase cancelarReservaUseCase)
    {
        _listarReservasUseCase = listarReservasUseCase;
        _cancelarReservaUseCase = cancelarReservaUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var reservas = await _listarReservasUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return Ok(reservas);
    }

    [HttpPatch("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cancelarReservaUseCase.ExecuteAsync(User.GetNegocioId(), id, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
