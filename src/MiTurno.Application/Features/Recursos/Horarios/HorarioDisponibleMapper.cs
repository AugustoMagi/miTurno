using MiTurno.Application.Features.Recursos.Horarios.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Recursos.Horarios;

internal static class HorarioDisponibleMapper
{
    public static HorarioDisponibleResponse ToResponse(this HorarioDisponible horario) => new(
        horario.Id,
        horario.RecursoId,
        horario.DiaSemana,
        horario.HoraInicio,
        horario.HoraFin);
}
