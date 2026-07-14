using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Recursos.Horarios.Dtos;

namespace MiTurno.Application.Features.Recursos.Horarios;

/// <summary>Lista las franjas horarias de un recurso, verificando que pertenezca al negocio autenticado.</summary>
public class ListarHorariosDisponiblesUseCase
{
    private readonly IRecursoRepository _recursoRepository;

    public ListarHorariosDisponiblesUseCase(IRecursoRepository recursoRepository)
    {
        _recursoRepository = recursoRepository;
    }

    public async Task<Result<IReadOnlyList<HorarioDisponibleResponse>>> ExecuteAsync(
        Guid negocioId, Guid recursoId, CancellationToken cancellationToken = default)
    {
        var recurso = await _recursoRepository.GetConHorariosYBloqueosAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocioId)
            return Result.Failure<IReadOnlyList<HorarioDisponibleResponse>>("Recurso no encontrado.");

        IReadOnlyList<HorarioDisponibleResponse> horarios = recurso.HorariosDisponibles
            .Select(h => h.ToResponse())
            .ToList();

        return Result.Success(horarios);
    }
}
