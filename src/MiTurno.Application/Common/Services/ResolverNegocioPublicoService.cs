using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Services;

/// <summary>
/// Resuelve un Negocio a partir de su slug público, aplicando el mismo criterio de acceso en
/// todos los puntos de entrada del flujo público (ver negocio, ver turnos, crear reserva): tiene
/// que estar Activo y, si tiene una Suscripcion asignada, que esté vigente. Un negocio sin ninguna
/// Suscripcion (todavía no se le asignó una, o se registró antes de que existiera este gating) se
/// considera con acceso permitido — no se bloquea retroactivamente.
/// No se usa en ConfirmarPagoUseCase/RechazarPagoUseCase/CancelarReservaClienteUseCase ni en el
/// webhook de pago: esos operan sobre una reserva ya creada y no deben trabarse si la suscripción
/// del negocio vence mientras un pago está en curso.
/// </summary>
public class ResolverNegocioPublicoService
{
    private readonly INegocioRepository _negocioRepository;
    private readonly ISuscripcionRepository _suscripcionRepository;

    public ResolverNegocioPublicoService(
        INegocioRepository negocioRepository, ISuscripcionRepository suscripcionRepository)
    {
        _negocioRepository = negocioRepository;
        _suscripcionRepository = suscripcionRepository;
    }

    public async Task<Result<Negocio>> ResolverAsync(string slug, CancellationToken cancellationToken = default)
    {
        var negocio = await _negocioRepository.GetBySlugAsync(slug, cancellationToken);
        if (negocio is null || !negocio.Activo)
            return Result.Failure<Negocio>("Negocio no encontrado.");

        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocio.Id, cancellationToken);
        if (suscripcion is not null && !suscripcion.EstaActiva)
            return Result.Failure<Negocio>("Negocio no encontrado.");

        return Result.Success(negocio);
    }
}
