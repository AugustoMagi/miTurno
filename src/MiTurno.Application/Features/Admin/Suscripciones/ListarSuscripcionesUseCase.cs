using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Suscripciones.Dtos;

namespace MiTurno.Application.Features.Admin.Suscripciones;

/// <summary>Todas las Suscripcion existentes, con los datos del negocio, para la gestión del SysAdmin.</summary>
public class ListarSuscripcionesUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly INegocioRepository _negocioRepository;

    public ListarSuscripcionesUseCase(
        ISuscripcionRepository suscripcionRepository, INegocioRepository negocioRepository)
    {
        _suscripcionRepository = suscripcionRepository;
        _negocioRepository = negocioRepository;
    }

    public async Task<IReadOnlyList<SuscripcionAdminResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var suscripciones = await _suscripcionRepository.GetAllAsync(cancellationToken);
        if (suscripciones.Count == 0)
            return [];

        var negociosPorId = new Dictionary<Guid, Domain.Entities.Negocio>();
        foreach (var negocioId in suscripciones.Select(s => s.NegocioId).Distinct())
        {
            var negocio = await _negocioRepository.GetByIdAsync(negocioId, cancellationToken);
            negociosPorId[negocioId] = negocio!;
        }

        return suscripciones
            .Select(s => s.ToResponse(negociosPorId[s.NegocioId]))
            .ToList();
    }
}
