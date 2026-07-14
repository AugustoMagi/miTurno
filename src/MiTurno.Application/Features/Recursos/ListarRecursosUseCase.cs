using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos.Dtos;

namespace MiTurno.Application.Features.Recursos;

/// <summary>Lista los recursos (canchas) del negocio del usuario autenticado.</summary>
public class ListarRecursosUseCase
{
    private readonly IRecursoRepository _recursoRepository;

    public ListarRecursosUseCase(IRecursoRepository recursoRepository)
    {
        _recursoRepository = recursoRepository;
    }

    public async Task<IReadOnlyList<RecursoResponse>> ExecuteAsync(
        Guid negocioId, CancellationToken cancellationToken = default)
    {
        var recursos = await _recursoRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        return recursos.Select(r => r.ToResponse()).ToList();
    }
}
