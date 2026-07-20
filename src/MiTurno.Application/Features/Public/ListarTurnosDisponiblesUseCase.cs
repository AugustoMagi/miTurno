using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Public.Dtos;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Public;

/// <summary>
/// Calcula, para un recurso y una fecha, los turnos libres a partir de sus horarios semanales
/// disponibles, descontando los bloqueos de esa fecha y las reservas ya tomadas.
/// </summary>
public class ListarTurnosDisponiblesUseCase
{
    private readonly ResolverNegocioPublicoService _resolverNegocioPublicoService;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IReservaRepository _reservaRepository;
    private readonly IClock _clock;

    public ListarTurnosDisponiblesUseCase(
        ResolverNegocioPublicoService resolverNegocioPublicoService,
        IRecursoRepository recursoRepository,
        IReservaRepository reservaRepository,
        IClock clock)
    {
        _resolverNegocioPublicoService = resolverNegocioPublicoService;
        _recursoRepository = recursoRepository;
        _reservaRepository = reservaRepository;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<TurnoDisponibleResponse>>> ExecuteAsync(
        string slug, Guid recursoId, DateOnly fecha, CancellationToken cancellationToken = default)
    {
        var ahora = _clock.Now;
        if (fecha < DateOnly.FromDateTime(ahora))
            return Result.Failure<IReadOnlyList<TurnoDisponibleResponse>>("No se pueden consultar turnos de fechas pasadas.");

        var negocioResult = await _resolverNegocioPublicoService.ResolverAsync(slug, cancellationToken);
        if (negocioResult.IsFailure)
            return Result.Failure<IReadOnlyList<TurnoDisponibleResponse>>(negocioResult.Error!);
        var negocio = negocioResult.Value;

        var recurso = await _recursoRepository.GetConHorariosYBloqueosAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocio.Id || !recurso.Activo)
            return Result.Failure<IReadOnlyList<TurnoDisponibleResponse>>("Recurso no encontrado.");

        if (recurso.BloqueosFecha.Any(b => b.Fecha == fecha))
            return Result.Success<IReadOnlyList<TurnoDisponibleResponse>>([]);

        var reservasDelDia = await _reservaRepository.GetByRecursoYFechaAsync(recursoId, fecha, cancellationToken);
        var reservasActivas = reservasDelDia.Where(r => r.Estado != EstadoReserva.Cancelada).ToList();

        var esHoy = fecha == DateOnly.FromDateTime(ahora);

        var turnos = new List<TurnoDisponibleResponse>();
        foreach (var horario in recurso.HorariosDisponibles.Where(h => h.DiaSemana == fecha.DayOfWeek))
        {
            var inicio = horario.HoraInicio;
            while (inicio + recurso.DuracionTurno <= horario.HoraFin)
            {
                var fin = inicio + recurso.DuracionTurno;

                var yaPaso = esHoy && inicio <= ahora.TimeOfDay;
                var ocupado = reservasActivas.Any(r => r.HoraInicio < fin && inicio < r.HoraFin);
                if (!yaPaso && !ocupado)
                    turnos.Add(new TurnoDisponibleResponse(inicio, fin));

                inicio = fin;
            }
        }

        return Result.Success<IReadOnlyList<TurnoDisponibleResponse>>(
            turnos.OrderBy(t => t.HoraInicio).ToList());
    }
}
