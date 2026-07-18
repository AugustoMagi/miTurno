using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Planes.Dtos;

namespace MiTurno.Application.Features.Admin.Planes;

/// <summary>Todos los Plan (activos e inactivos), para la gestión del SysAdmin.</summary>
public class ListarPlanesUseCase
{
    private readonly IPlanRepository _planRepository;

    public ListarPlanesUseCase(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<IReadOnlyList<PlanResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var planes = await _planRepository.GetAllAsync(cancellationToken);
        return planes.Select(p => p.ToResponse()).ToList();
    }
}
