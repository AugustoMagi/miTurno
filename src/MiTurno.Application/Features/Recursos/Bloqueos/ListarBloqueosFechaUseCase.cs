using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Recursos.Bloqueos.Dtos;

namespace MiTurno.Application.Features.Recursos.Bloqueos;

/// <summary>Lista los bloqueos de fecha de un recurso, verificando que pertenezca al negocio autenticado.</summary>
public class ListarBloqueosFechaUseCase
{
    private readonly IRecursoRepository _recursoRepository;

    public ListarBloqueosFechaUseCase(IRecursoRepository recursoRepository)
    {
        _recursoRepository = recursoRepository;
    }

    public async Task<Result<IReadOnlyList<BloqueoFechaResponse>>> ExecuteAsync(
        Guid negocioId, Guid recursoId, CancellationToken cancellationToken = default)
    {
        var recurso = await _recursoRepository.GetConHorariosYBloqueosAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocioId)
            return Result.Failure<IReadOnlyList<BloqueoFechaResponse>>("Recurso no encontrado.");

        IReadOnlyList<BloqueoFechaResponse> bloqueos = recurso.BloqueosFecha
            .OrderBy(b => b.Fecha)
            .Select(b => b.ToResponse())
            .ToList();

        return Result.Success(bloqueos);
    }
}
