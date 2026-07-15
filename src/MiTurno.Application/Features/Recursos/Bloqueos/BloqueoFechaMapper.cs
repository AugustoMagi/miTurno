using MiTurno.Application.Features.Recursos.Bloqueos.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Recursos.Bloqueos;

internal static class BloqueoFechaMapper
{
    public static BloqueoFechaResponse ToResponse(
        this BloqueoFecha bloqueo, IReadOnlyList<ReservaAfectadaResponse>? reservasAfectadas = null) => new(
        bloqueo.Id,
        bloqueo.RecursoId,
        bloqueo.Fecha,
        bloqueo.Motivo,
        reservasAfectadas ?? []);
}
