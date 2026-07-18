using MiTurno.Application.Features.Admin.Planes.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Admin.Planes;

internal static class PlanMapper
{
    public static PlanResponse ToResponse(this Plan plan) => new(
        plan.Id,
        plan.Nombre,
        plan.Precio,
        plan.Periodicidad,
        plan.LimiteRecursos,
        plan.LimiteReservasPorMes,
        plan.Activo,
        plan.EsPlanDePrueba);
}
