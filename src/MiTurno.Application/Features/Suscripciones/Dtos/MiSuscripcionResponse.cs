using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Suscripciones.Dtos;

public record MiSuscripcionResponse(
    Guid Id,
    string PlanNombre,
    decimal PlanPrecio,
    Periodicidad Periodicidad,
    EstadoSuscripcion Estado,
    DateTime FechaProximoVencimiento,
    bool EstaActiva);
