using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Public.Dtos;

public record PlanPublicoResponse(
    Guid Id,
    string Nombre,
    decimal Precio,
    Periodicidad Periodicidad,
    int LimiteRecursos,
    int LimiteReservasPorMes,
    bool EsPlanDePrueba);
