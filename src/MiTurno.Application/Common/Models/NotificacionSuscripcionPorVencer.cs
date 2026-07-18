namespace MiTurno.Application.Common.Models;

/// <summary>Datos para avisarle al dueño del negocio que su suscripción está por vencer.</summary>
public record NotificacionSuscripcionPorVencer(
    string NegocioEmail, string NegocioNombre, string PlanNombre, DateTime FechaVencimiento);
