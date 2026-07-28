using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Common.Interfaces;

/// <summary>
/// Puerto hacia el cobro recurrente (Preapproval) de Mercado Pago: a diferencia de IPagoGateway
/// (una preferencia de pago único), esto autoriza a la plataforma a cobrarle al negocio
/// automáticamente cada período, sin que tenga que volver a pagar a mano. Nunca lanza excepciones
/// ante fallas de red o rechazos de la pasarela.
/// </summary>
public interface IPagoRecurrenteGateway
{
    Task<Result<PreapprovalCreadoResult>> CrearPreapprovalAsync(
        CrearPreapprovalRequest request, CancellationToken cancellationToken = default);

    Task<Result<PreapprovalEstadoResult>> ObtenerPreapprovalAsync(
        string accessToken, string preapprovalId, CancellationToken cancellationToken = default);

    Task<Result> CancelarPreapprovalAsync(
        string accessToken, string preapprovalId, CancellationToken cancellationToken = default);

    Task<Result> ActualizarMontoPreapprovalAsync(
        string accessToken, string preapprovalId, decimal nuevoMonto, CancellationToken cancellationToken = default);

    Task<Result<CargoRecurrenteResult>> ObtenerCargoRecurrenteAsync(
        string accessToken, string pagoExternoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Id del cargo procesado/aprobado más reciente de una Preapproval, o null si todavía no tiene
    /// ninguno. Se usa para reconciliar cuando el webhook de Mercado Pago no llegó (ver
    /// ObtenerMiSuscripcionUseCase): en vez de depender solo de la notificación, se puede
    /// repreguntar y procesar el cargo como si el webhook hubiera llegado recién.
    /// </summary>
    Task<Result<string?>> BuscarUltimoCargoIdAsync(
        string accessToken, string preapprovalId, CancellationToken cancellationToken = default);
}
