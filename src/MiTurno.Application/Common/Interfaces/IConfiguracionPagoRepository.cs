using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface IConfiguracionPagoRepository : IRepository<ConfiguracionPago>
{
    /// <summary>La cuenta de cobro activa del negocio (Mercado Pago/Stripe) que recibe los pagos de sus reservas.</summary>
    Task<ConfiguracionPago?> GetActivaByNegocioIdAsync(Guid negocioId, CancellationToken cancellationToken = default);
}
