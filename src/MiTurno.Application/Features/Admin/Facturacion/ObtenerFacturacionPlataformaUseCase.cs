using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Facturacion.Dtos;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Admin.Facturacion;

/// <summary>
/// Ingresos de la plataforma (lo que los negocios pagan por su Suscripcion), no confundir con el
/// volumen de reservas que cada negocio le cobra a sus propios clientes (ver ObtenerEstadisticasOcupacionUseCase).
/// Solo cuenta PagoSuscripcion en estado Aprobado, opcionalmente acotado a un rango de fechas.
/// </summary>
public class ObtenerFacturacionPlataformaUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly INegocioRepository _negocioRepository;

    public ObtenerFacturacionPlataformaUseCase(
        ISuscripcionRepository suscripcionRepository, INegocioRepository negocioRepository)
    {
        _suscripcionRepository = suscripcionRepository;
        _negocioRepository = negocioRepository;
    }

    public async Task<FacturacionPlataformaResponse> ExecuteAsync(
        DateOnly? desde, DateOnly? hasta, CancellationToken cancellationToken = default)
    {
        var suscripciones = await _suscripcionRepository.GetAllAsync(cancellationToken);

        var pagos = suscripciones
            .SelectMany(s => s.Pagos
                .Where(p => p.Estado == EstadoPago.Aprobado)
                .Select(p => new { Suscripcion = s, Pago = p }))
            .Where(x => desde is null || DateOnly.FromDateTime(x.Pago.Fecha) >= desde)
            .Where(x => hasta is null || DateOnly.FromDateTime(x.Pago.Fecha) <= hasta)
            .ToList();

        if (pagos.Count == 0)
            return new FacturacionPlataformaResponse(0, 0, [], []);

        var porPlan = pagos
            .GroupBy(x => x.Suscripcion.PlanId)
            .Select(g => new FacturacionPorPlanDto(
                g.Key, g.First().Suscripcion.Plan.Nombre, g.Sum(x => x.Pago.Monto), g.Count()))
            .OrderByDescending(d => d.Total)
            .ToList();

        var porNegocio = new List<FacturacionPorNegocioDto>();
        foreach (var grupo in pagos.GroupBy(x => x.Suscripcion.NegocioId))
        {
            var negocio = await _negocioRepository.GetByIdAsync(grupo.Key, cancellationToken);
            porNegocio.Add(new FacturacionPorNegocioDto(
                grupo.Key, negocio?.Nombre ?? "(negocio eliminado)", grupo.Sum(x => x.Pago.Monto), grupo.Count()));
        }
        porNegocio = porNegocio.OrderByDescending(d => d.Total).ToList();

        return new FacturacionPlataformaResponse(
            pagos.Sum(x => x.Pago.Monto), pagos.Count, porPlan, porNegocio);
    }
}
