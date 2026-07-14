using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Recursos.Dtos;

namespace MiTurno.Application.Features.Recursos;

/// <summary>Obtiene un recurso puntual, verificando que pertenezca al negocio del usuario autenticado.</summary>
public class ObtenerRecursoUseCase
{
    private readonly IRecursoRepository _recursoRepository;

    public ObtenerRecursoUseCase(IRecursoRepository recursoRepository)
    {
        _recursoRepository = recursoRepository;
    }

    public async Task<Result<RecursoResponse>> ExecuteAsync(
        Guid negocioId, Guid recursoId, CancellationToken cancellationToken = default)
    {
        var recurso = await _recursoRepository.GetByIdAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocioId)
            return Result.Failure<RecursoResponse>("Recurso no encontrado.");

        return Result.Success(recurso.ToResponse());
    }
}
