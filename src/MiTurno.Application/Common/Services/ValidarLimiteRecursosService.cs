using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Common.Services;

/// <summary>
/// Chequea si un negocio puede sumar una cancha activa más (al crear una nueva o al reactivar una
/// desactivada) según el límite de su plan vigente. Un negocio sin Suscripcion asignada
/// (retrocompatibilidad con negocios de antes de esta feature) no tiene límite. Se cuentan solo los
/// recursos activos: uno desactivado no ocupa cupo.
/// </summary>
public class ValidarLimiteRecursosService
{
    private readonly IRecursoRepository _recursoRepository;
    private readonly ISuscripcionRepository _suscripcionRepository;

    public ValidarLimiteRecursosService(
        IRecursoRepository recursoRepository, ISuscripcionRepository suscripcionRepository)
    {
        _recursoRepository = recursoRepository;
        _suscripcionRepository = suscripcionRepository;
    }

    public async Task<Result> ValidarAsync(Guid negocioId, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is null)
            return Result.Success();

        var recursos = await _recursoRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        var recursosActivos = recursos.Count(r => r.Activo);
        if (recursosActivos < suscripcion.Plan.LimiteRecursos)
            return Result.Success();

        var limite = suscripcion.Plan.LimiteRecursos;
        return Result.Failure(
            $"Alcanzaste el límite de {limite} cancha{(limite == 1 ? "" : "s")} de tu plan actual ({suscripcion.Plan.Nombre}). Cambiá de plan en \"Mi suscripción\" para agregar más.");
    }
}
