using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Admin.Planes.Dtos;

public record ActualizarPlanRequest(
    string Nombre,
    decimal Precio,
    Periodicidad Periodicidad,
    int LimiteRecursos,
    int LimiteReservasPorMes);
