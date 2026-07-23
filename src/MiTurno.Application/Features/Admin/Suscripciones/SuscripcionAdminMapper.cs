using MiTurno.Application.Features.Admin.Suscripciones.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Admin.Suscripciones;

internal static class SuscripcionAdminMapper
{
    public static SuscripcionAdminResponse ToResponse(this Suscripcion suscripcion, Negocio negocio) => new(
        suscripcion.Id, suscripcion.NegocioId, negocio.Nombre, negocio.Email,
        suscripcion.PlanId, suscripcion.Plan.Nombre, suscripcion.Plan.Precio,
        suscripcion.Estado, suscripcion.FechaInicio, suscripcion.FechaProximoVencimiento,
        suscripcion.MercadoPagoPreapprovalId is not null);
}
