using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Suscripciones.Dtos;

public record MiSuscripcionResponse(
    Guid Id,
    Guid PlanId,
    string PlanNombre,
    decimal PlanPrecio,
    Periodicidad Periodicidad,
    EstadoSuscripcion Estado,
    DateTime FechaProximoVencimiento,
    bool EstaActiva);
