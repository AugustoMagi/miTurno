using MiTurno.Domain.Entities;

namespace MiTurno.Application.Common.Interfaces;

public interface IConfiguracionPagoRepository : IRepository<ConfiguracionPago>
{
    /// <summary>La cuenta de cobro activa del negocio (Mercado Pago/Stripe) que recibe los pagos de sus reservas.</summary>
    Task<ConfiguracionPago?> GetActivaByNegocioIdAsync(Guid negocioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Conexiones activas por OAuth cuyo AccessToken vence antes de <paramref name="antesDe"/>, para
    /// renovarlas de antemano y que nunca lleguen vencidas a CrearReservaUseCase/el webhook de pago.
    /// </summary>
    Task<IReadOnlyList<ConfiguracionPago>> GetConexionesOAuthPorVencerAsync(DateTime antesDe, CancellationToken cancellationToken = default);
}
