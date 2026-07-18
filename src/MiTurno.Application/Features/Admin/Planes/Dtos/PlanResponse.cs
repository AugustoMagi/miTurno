using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Admin.Planes.Dtos;

public record PlanResponse(
    Guid Id,
    string Nombre,
    decimal Precio,
    Periodicidad Periodicidad,
    int LimiteRecursos,
    int LimiteReservasPorMes,
    bool Activo,
    bool EsPlanDePrueba);
