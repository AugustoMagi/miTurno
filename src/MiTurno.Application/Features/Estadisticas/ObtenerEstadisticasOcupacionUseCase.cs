using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Estadisticas.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Estadisticas;

/// <summary>
/// Calcula ingresos y ocupación por recurso a partir de las reservas del negocio del usuario
/// autenticado, opcionalmente acotado a un rango de fechas. Ingresos solo cuenta reservas
/// Confirmada/Completada (no depende del estado del Pago, que se concilia aparte).
/// </summary>
public class ObtenerEstadisticasOcupacionUseCase
{
    private readonly IReservaRepository _reservaRepository;
    private readonly IRecursoRepository _recursoRepository;

    public ObtenerEstadisticasOcupacionUseCase(
        IReservaRepository reservaRepository,
        IRecursoRepository recursoRepository)
    {
        _reservaRepository = reservaRepository;
        _recursoRepository = recursoRepository;
    }

    public async Task<EstadisticasOcupacionResponse> ExecuteAsync(
        Guid negocioId, DateOnly? desde, DateOnly? hasta, CancellationToken cancellationToken = default)
    {
        var reservas = (await _reservaRepository.GetByNegocioIdAsync(negocioId, cancellationToken))
            .Where(r => desde is null || r.Fecha >= desde)
            .Where(r => hasta is null || r.Fecha <= hasta)
            .ToList();

        var ingresosTotales = reservas
            .Where(r => r.Estado is EstadoReserva.Confirmada or EstadoReserva.Completada)
            .Sum(r => r.PrecioTotal);

        var ocupacionPorRecurso = new List<OcupacionRecursoResponse>();
        if (reservas.Count > 0)
        {
            var recursos = await _recursoRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
            var recursoNombrePorId = recursos.ToDictionary(r => r.Id, r => r.Nombre);

            ocupacionPorRecurso = reservas
                .GroupBy(r => r.RecursoId)
                .Select(g => new OcupacionRecursoResponse(
                    g.Key, recursoNombrePorId[g.Key], g.Count(), ContarPorEstado(g.ToList())))
                .OrderByDescending(o => o.TotalReservas)
                .ToList();
        }

        return new EstadisticasOcupacionResponse(
            ingresosTotales, reservas.Count, ContarPorEstado(reservas), ocupacionPorRecurso);
    }

    private static IReadOnlyList<ReservasPorEstadoDto> ContarPorEstado(IReadOnlyList<Reserva> reservas) =>
        reservas
            .GroupBy(r => r.Estado)
            .Select(g => new ReservasPorEstadoDto(g.Key, g.Count()))
            .OrderBy(d => d.Estado)
            .ToList();
}
