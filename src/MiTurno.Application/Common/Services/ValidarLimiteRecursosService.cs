using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Domain.Entities;

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

    /// <summary>
    /// Chequea si un negocio puede pasarse a <paramref name="nuevoPlan"/> según cuántas canchas activas
    /// tiene hoy. A diferencia de <see cref="ValidarAsync"/> (que compara contra el plan vigente para
    /// sumar una cancha más), acá se compara la cantidad actual contra el límite del plan de destino:
    /// si el negocio ya tiene más canchas activas de las que ese plan permite, se bloquea el cambio en
    /// vez de dejarlo desactualizado silenciosamente.
    /// </summary>
    public async Task<Result> ValidarCambioDePlanAsync(
        Guid negocioId, Plan nuevoPlan, CancellationToken cancellationToken = default)
    {
        var recursos = await _recursoRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        var recursosActivos = recursos.Count(r => r.Activo);
        if (recursosActivos <= nuevoPlan.LimiteRecursos)
            return Result.Success();

        return Result.Failure(
            $"Tenés {recursosActivos} canchas activas y el plan \"{nuevoPlan.Nombre}\" permite hasta {nuevoPlan.LimiteRecursos}. Desactivá canchas antes de cambiar a este plan.");
    }
}
