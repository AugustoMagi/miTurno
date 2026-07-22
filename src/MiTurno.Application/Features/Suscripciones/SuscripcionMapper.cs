using MiTurno.Application.Features.Suscripciones.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Suscripciones;

internal static class SuscripcionMapper
{
    public static MiSuscripcionResponse ToMiSuscripcionResponse(this Suscripcion suscripcion) => new(
        suscripcion.Id,
        suscripcion.PlanId,
        suscripcion.Plan.Nombre,
        suscripcion.Plan.Precio,
        suscripcion.Plan.Periodicidad,
        suscripcion.Estado,
        suscripcion.FechaProximoVencimiento,
        suscripcion.EstaActiva,
        suscripcion.MercadoPagoPreapprovalId is not null);
}
