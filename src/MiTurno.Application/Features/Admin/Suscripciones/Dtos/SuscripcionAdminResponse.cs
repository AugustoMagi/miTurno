using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Admin.Suscripciones.Dtos;

public record SuscripcionAdminResponse(
    Guid Id,
    Guid NegocioId,
    string NegocioNombre,
    string NegocioEmail,
    Guid PlanId,
    string PlanNombre,
    decimal PlanPrecio,
    EstadoSuscripcion Estado,
    DateTime FechaInicio,
    DateTime FechaProximoVencimiento,
    bool CobroAutomaticoActivo);
