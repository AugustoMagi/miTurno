using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Public.Dtos;

namespace MiTurno.Application.Features.Public;

/// <summary>Catálogo de planes activos para la landing pública, sin datos administrativos.</summary>
public class ListarPlanesPublicosUseCase
{
    private readonly IPlanRepository _planRepository;

    public ListarPlanesPublicosUseCase(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<IReadOnlyList<PlanPublicoResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var planes = await _planRepository.GetActivosAsync(cancellationToken);
        return planes
            .OrderBy(p => p.Precio)
            .Select(p => new PlanPublicoResponse(
                p.Id, p.Nombre, p.Precio, p.Periodicidad, p.LimiteRecursos, p.LimiteReservasPorMes, p.EsPlanDePrueba))
            .ToList();
    }
}
