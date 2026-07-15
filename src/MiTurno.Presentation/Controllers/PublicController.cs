using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Public;
using MiTurno.Application.Features.Public.Dtos;

namespace MiTurno.Presentation.Controllers;

/// <summary>Endpoints públicos consumidos desde el link de reserva del negocio (ej. su bio de Instagram), sin autenticación.</summary>
[ApiController]
[Route("api/public/negocios")]
public class PublicController : ControllerBase
{
    private readonly ObtenerNegocioPublicoUseCase _obtenerNegocioPublicoUseCase;
    private readonly ListarTurnosDisponiblesUseCase _listarTurnosDisponiblesUseCase;
    private readonly CrearReservaUseCase _crearReservaUseCase;
    private readonly ConfirmarPagoUseCase _confirmarPagoUseCase;
    private readonly RechazarPagoUseCase _rechazarPagoUseCase;

    public PublicController(
        ObtenerNegocioPublicoUseCase obtenerNegocioPublicoUseCase,
        ListarTurnosDisponiblesUseCase listarTurnosDisponiblesUseCase,
        CrearReservaUseCase crearReservaUseCase,
        ConfirmarPagoUseCase confirmarPagoUseCase,
        RechazarPagoUseCase rechazarPagoUseCase)
    {
        _obtenerNegocioPublicoUseCase = obtenerNegocioPublicoUseCase;
        _listarTurnosDisponiblesUseCase = listarTurnosDisponiblesUseCase;
        _crearReservaUseCase = crearReservaUseCase;
        _confirmarPagoUseCase = confirmarPagoUseCase;
        _rechazarPagoUseCase = rechazarPagoUseCase;
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

    [HttpPost("{slug}/recursos/{recursoId:guid}/reservas")]
    public async Task<IActionResult> CrearReserva(
        string slug, Guid recursoId, CrearReservaRequest request, CancellationToken cancellationToken)
    {
        var result = await _crearReservaUseCase.ExecuteAsync(slug, recursoId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{slug}/reservas/{reservaId:guid}/pago/confirmar")]
    public async Task<IActionResult> ConfirmarPago(string slug, Guid reservaId, CancellationToken cancellationToken)
    {
        var result = await _confirmarPagoUseCase.ExecuteAsync(slug, reservaId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{slug}/reservas/{reservaId:guid}/pago/rechazar")]
    public async Task<IActionResult> RechazarPago(string slug, Guid reservaId, CancellationToken cancellationToken)
    {
        var result = await _rechazarPagoUseCase.ExecuteAsync(slug, reservaId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
