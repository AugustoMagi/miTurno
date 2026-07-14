using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Public.Dtos;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Public;

/// <summary>
/// Calcula, para un recurso y una fecha, los turnos libres a partir de sus horarios semanales
/// disponibles, descontando los bloqueos de esa fecha y las reservas ya tomadas.
/// </summary>
public class ListarTurnosDisponiblesUseCase
{
    private readonly INegocioRepository _negocioRepository;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IReservaRepository _reservaRepository;

    public ListarTurnosDisponiblesUseCase(
        INegocioRepository negocioRepository,
        IRecursoRepository recursoRepository,
        IReservaRepository reservaRepository)
    {
        _negocioRepository = negocioRepository;
        _recursoRepository = recursoRepository;
        _reservaRepository = reservaRepository;
    }

    public async Task<Result<IReadOnlyList<TurnoDisponibleResponse>>> ExecuteAsync(
        string slug, Guid recursoId, DateOnly fecha, CancellationToken cancellationToken = default)
    {
        if (fecha < DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Failure<IReadOnlyList<TurnoDisponibleResponse>>("No se pueden consultar turnos de fechas pasadas.");

        var negocio = await _negocioRepository.GetBySlugAsync(slug, cancellationToken);
        if (negocio is null || !negocio.Activo)
            return Result.Failure<IReadOnlyList<TurnoDisponibleResponse>>("Negocio no encontrado.");

        var recurso = await _recursoRepository.GetConHorariosYBloqueosAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocio.Id || !recurso.Activo)
            return Result.Failure<IReadOnlyList<TurnoDisponibleResponse>>("Recurso no encontrado.");

        if (recurso.BloqueosFecha.Any(b => b.Fecha == fecha))
            return Result.Success<IReadOnlyList<TurnoDisponibleResponse>>([]);

        var reservasDelDia = await _reservaRepository.GetByRecursoYFechaAsync(recursoId, fecha, cancellationToken);
        var reservasActivas = reservasDelDia.Where(r => r.Estado != EstadoReserva.Cancelada).ToList();

        var turnos = new List<TurnoDisponibleResponse>();
        foreach (var horario in recurso.HorariosDisponibles.Where(h => h.DiaSemana == fecha.DayOfWeek))
        {
            var inicio = horario.HoraInicio;
            while (inicio + recurso.DuracionTurno <= horario.HoraFin)
            {
                var fin = inicio + recurso.DuracionTurno;

                var ocupado = reservasActivas.Any(r => r.HoraInicio < fin && inicio < r.HoraFin);
                if (!ocupado)
                    turnos.Add(new TurnoDisponibleResponse(inicio, fin));

                inicio = fin;
            }
        }

        return Result.Success<IReadOnlyList<TurnoDisponibleResponse>>(
            turnos.OrderBy(t => t.HoraInicio).ToList());
    }
}
