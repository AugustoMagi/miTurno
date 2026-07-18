using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Estadisticas.Dtos;

public record EstadisticasOcupacionResponse(
    decimal IngresosTotales,
    int TotalReservas,
    IReadOnlyList<ReservasPorEstadoDto> ReservasPorEstado,
    IReadOnlyList<OcupacionRecursoResponse> OcupacionPorRecurso);

public record OcupacionRecursoResponse(
    Guid RecursoId,
    string RecursoNombre,
    int TotalReservas,
    IReadOnlyList<ReservasPorEstadoDto> ReservasPorEstado);

public record ReservasPorEstadoDto(EstadoReserva Estado, int Cantidad);
