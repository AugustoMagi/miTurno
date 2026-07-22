namespace MiTurno.Application.Features.Admin.Facturacion.Dtos;

public record FacturacionPlataformaResponse(
    decimal TotalFacturado,
    int CantidadPagos,
    IReadOnlyList<FacturacionPorPlanDto> PorPlan,
    IReadOnlyList<FacturacionPorNegocioDto> PorNegocio);

public record FacturacionPorPlanDto(Guid PlanId, string PlanNombre, decimal Total, int CantidadPagos);

public record FacturacionPorNegocioDto(Guid NegocioId, string NegocioNombre, decimal Total, int CantidadPagos);
