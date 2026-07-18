using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.Suscripciones;

/// <summary>
/// Avisa por email al dueño de cada negocio cuya suscripción está por vencer dentro de
/// <paramref name="diasAnticipacion"/> días, y marca el aviso como enviado para no repetirlo
/// hasta la próxima renovación. Pensado para dispararse desde una tarea programada, no desde un
/// endpoint HTTP.
/// </summary>
public class NotificarSuscripcionesPorVencerUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly INegocioRepository _negocioRepository;
    private readonly IEmailNotificador _emailNotificador;
    private readonly IUnitOfWork _unitOfWork;

    public NotificarSuscripcionesPorVencerUseCase(
        ISuscripcionRepository suscripcionRepository,
        INegocioRepository negocioRepository,
        IEmailNotificador emailNotificador,
        IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _negocioRepository = negocioRepository;
        _emailNotificador = emailNotificador;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> ExecuteAsync(int diasAnticipacion = 3, CancellationToken cancellationToken = default)
    {
        var limite = DateTime.UtcNow.AddDays(diasAnticipacion);
        var suscripciones = await _suscripcionRepository.GetPendientesDeNotificarVencimientoAsync(limite, cancellationToken);

        var notificadas = 0;
        foreach (var suscripcion in suscripciones)
        {
            var negocio = await _negocioRepository.GetByIdAsync(suscripcion.NegocioId, cancellationToken);
            if (negocio is null)
                continue;

            await _emailNotificador.NotificarSuscripcionPorVencerAsync(
                new NotificacionSuscripcionPorVencer(
                    negocio.Email, negocio.Nombre, suscripcion.Plan.Nombre, suscripcion.FechaProximoVencimiento),
                cancellationToken);

            suscripcion.MarcarNotificacionVencimientoEnviada();
            _suscripcionRepository.Update(suscripcion);
            notificadas++;
        }

        if (notificadas > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(notificadas);
    }
}
