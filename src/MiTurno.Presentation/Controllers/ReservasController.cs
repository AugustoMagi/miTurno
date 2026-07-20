using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Reservas;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/reservas")]
[Authorize(Roles = "Owner,Empleado")]
public class ReservasController : ControllerBase
{
    private readonly ListarReservasUseCase _listarReservasUseCase;
    private readonly CancelarReservaUseCase _cancelarReservaUseCase;
    private readonly ConfirmarPagoUseCase _confirmarPagoUseCase;
    private readonly RechazarPagoUseCase _rechazarPagoUseCase;

    public ReservasController(
        ListarReservasUseCase listarReservasUseCase,
        CancelarReservaUseCase cancelarReservaUseCase,
        ConfirmarPagoUseCase confirmarPagoUseCase,
        RechazarPagoUseCase rechazarPagoUseCase)
    {
        _listarReservasUseCase = listarReservasUseCase;
        _cancelarReservaUseCase = cancelarReservaUseCase;
        _confirmarPagoUseCase = confirmarPagoUseCase;
        _rechazarPagoUseCase = rechazarPagoUseCase;
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

    /// <summary>Confirmación manual del pago (transferencia/alias) por parte del dueño o un empleado.</summary>
    [HttpPatch("{id:guid}/pago/confirmar")]
    public async Task<IActionResult> ConfirmarPago(Guid id, CancellationToken cancellationToken)
    {
        var result = await _confirmarPagoUseCase.ExecuteAsync(User.GetNegocioId(), id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/pago/rechazar")]
    public async Task<IActionResult> RechazarPago(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rechazarPagoUseCase.ExecuteAsync(User.GetNegocioId(), id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
