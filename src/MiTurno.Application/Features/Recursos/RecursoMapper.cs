using MiTurno.Application.Features.Recursos.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Recursos;

internal static class RecursoMapper
{
    public static RecursoResponse ToResponse(this Recurso recurso) => new(
        recurso.Id,
        recurso.NegocioId,
        recurso.Nombre,
        recurso.Tipo,
        (int)recurso.DuracionTurno.TotalMinutes,
        recurso.Precio,
        recurso.Activo);
}
