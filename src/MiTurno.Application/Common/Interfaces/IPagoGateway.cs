using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Common.Interfaces;

/// <summary>
/// Puerto hacia la pasarela de pago real (hoy Mercado Pago) configurada en el ConfiguracionPago del
/// negocio. No lanza excepciones ante fallas de red o rechazos de la pasarela: siempre informa el
/// resultado vía Result para que el llamador decida cómo degradar (ver CrearReservaUseCase).
/// </summary>
public interface IPagoGateway
{
    Task<Result<PreferenciaPagoResult>> CrearPreferenciaAsync(
        CrearPreferenciaPagoRequest request, CancellationToken cancellationToken = default);

    Task<Result<EstadoPagoExternoResult>> ObtenerEstadoPagoAsync(
        string accessToken, string pagoExternoId, CancellationToken cancellationToken = default);
}
