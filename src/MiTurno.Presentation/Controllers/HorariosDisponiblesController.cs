using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Recursos.Horarios;
using MiTurno.Application.Features.Recursos.Horarios.Dtos;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/recursos/{recursoId:guid}/horarios")]
[Authorize(Roles = "Owner,Empleado")]
public class HorariosDisponiblesController : ControllerBase
{
    private readonly AgregarHorarioDisponibleUseCase _agregarHorarioDisponibleUseCase;
    private readonly ListarHorariosDisponiblesUseCase _listarHorariosDisponiblesUseCase;
    private readonly EliminarHorarioDisponibleUseCase _eliminarHorarioDisponibleUseCase;

    public HorariosDisponiblesController(
        AgregarHorarioDisponibleUseCase agregarHorarioDisponibleUseCase,
        ListarHorariosDisponiblesUseCase listarHorariosDisponiblesUseCase,
        EliminarHorarioDisponibleUseCase eliminarHorarioDisponibleUseCase)
    {
        _agregarHorarioDisponibleUseCase = agregarHorarioDisponibleUseCase;
        _listarHorariosDisponiblesUseCase = listarHorariosDisponiblesUseCase;
        _eliminarHorarioDisponibleUseCase = eliminarHorarioDisponibleUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> Agregar(Guid recursoId, AgregarHorarioDisponibleRequest request, CancellationToken cancellationToken)
    {
        var result = await _agregarHorarioDisponibleUseCase.ExecuteAsync(User.GetNegocioId(), recursoId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> Listar(Guid recursoId, CancellationToken cancellationToken)
    {
        var result = await _listarHorariosDisponiblesUseCase.ExecuteAsync(User.GetNegocioId(), recursoId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpDelete("{horarioId:guid}")]
    public async Task<IActionResult> Eliminar(Guid recursoId, Guid horarioId, CancellationToken cancellationToken)
    {
        var result = await _eliminarHorarioDisponibleUseCase.ExecuteAsync(User.GetNegocioId(), recursoId, horarioId, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
