using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Suscripciones;

/// <summary>
/// Arranca el cobro recurrente (Preapproval) de la suscripción SaaS del negocio, cobrado con la
/// cuenta de Mercado Pago de la propia plataforma MiTurno (no la del negocio): a partir de acá,
/// Mercado Pago le cobra automáticamente cada período sin que el negocio tenga que volver a pagar
/// a mano. Reemplaza el flujo anterior de preferencia de pago único.
/// </summary>
public class IniciarSuscripcionMercadoPagoUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly INegocioRepository _negocioRepository;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway;
    private readonly IFrontendConfiguracion _frontendConfiguracion;
    private readonly IUnitOfWork _unitOfWork;

    public IniciarSuscripcionMercadoPagoUseCase(
        ISuscripcionRepository suscripcionRepository,
        INegocioRepository negocioRepository,
        IPlataformaPagoConfiguracion plataformaPagoConfiguracion,
        IPagoRecurrenteGateway pagoRecurrenteGateway,
        IFrontendConfiguracion frontendConfiguracion,
        IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _negocioRepository = negocioRepository;
        _plataformaPagoConfiguracion = plataformaPagoConfiguracion;
        _pagoRecurrenteGateway = pagoRecurrenteGateway;
        _frontendConfiguracion = frontendConfiguracion;
        _unitOfWork = unitOfWork;
    }

    /// <param name="cobrarInmediato">
    /// True cuando se activa el cobro automático justo después de cambiar de plan: ahí se cobra
    /// desde ya (el negocio decidió pagar más/menos ya mismo, no seguir esperando el período viejo).
    /// False (default) para reactivarlo sobre el mismo plan de siempre (ej. desde la prueba gratis, o
    /// tras una Preapproval cancelada del todo): ahí se respeta el período ya vigente y no se cobra
    /// hasta que termine.
    /// </param>
    public async Task<Result<IniciarSuscripcionMercadoPagoResponse>> ExecuteAsync(
        Guid negocioId, string webhookBaseUrl, bool cobrarInmediato = false, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Todavía no tenés una suscripción asignada.");

        if (suscripcion.Plan.Precio <= 0)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Este plan no requiere cobro.");

        if (suscripcion.MercadoPagoPreapprovalId is not null)
        {
            var estadoResult = await _pagoRecurrenteGateway.ObtenerPreapprovalAsync(
                _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);

            // "cancelled": el negocio la canceló desde su propia cuenta de MP sin que el webhook nos
            // avise. "pending": nunca se llegó a autorizar (cerró el checkout sin completarlo) — no
            // hay nada activo ahí que proteger, y encima MP rechaza cancelar algo que nunca se
            // autorizó. En ninguno de los dos casos hay que bloquear ni intentar cancelarla: se suelta
            // directo y se sigue, en vez de dejar al negocio sin poder activar nunca más el cobro
            // automático por una Preapproval vieja que quedó a mitad de camino.
            var noHayNadaQueProteger = estadoResult.IsSuccess &&
                (estadoResult.Value.Status == "cancelled" || estadoResult.Value.Status == "pending");

            if (!noHayNadaQueProteger)
            {
                // Sin cobrarInmediato esto es "activar/reanudar el cobro de siempre" — si ya hay una
                // Preapproval viva (autorizada o pausada) no hay nada más que hacer, así que se bloquea
                // en vez de duplicarla. Con cobrarInmediato (viene de un cambio de plan) la vieja quedó
                // a otro precio: se cancela para autorizar una nueva a este plan.
                if (!cobrarInmediato)
                    return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Ya tenés el cobro automático de Mercado Pago activado.");

                var cancelacionResult = await _pagoRecurrenteGateway.CancelarPreapprovalAsync(
                    _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);
                if (cancelacionResult.IsFailure)
                    return Result.Failure<IniciarSuscripcionMercadoPagoResponse>(cancelacionResult.Error!);
            }

            suscripcion.QuitarPreapproval();
        }

        var negocio = await _negocioRepository.GetByIdAsync(negocioId, cancellationToken);
        if (negocio is null)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Negocio no encontrado.");

        var notificationUrl = $"{webhookBaseUrl}/api/public/suscripciones/{suscripcion.Id}/webhook/recurrente";
        var backUrl = $"{_frontendConfiguracion.BaseUrl}/panel/suscripcion?mp=vuelta";

        // El primer cobro no debe salir el día que se autoriza la Preapproval si se está retomando el
        // mismo plan de siempre (de prueba, o pago pero con el cobro automático apagado): ahí se
        // respeta el período ya vigente (start_date = esa fecha futura). Si en cambio se acaba de
        // cambiar de plan (cobrarInmediato) o ya no queda período que honrar, no se manda start_date
        // en absoluto — dejar que Mercado Pago cobre "ahora" por su cuenta, en vez de calcular un
        // "ahora" acá y mandarlo (la latencia de red puede hacer que ya esté en el pasado para cuando
        // MP lo valida, y lo rechace con "cannot be a past date").
        DateTime? fechaInicio = !cobrarInmediato && suscripcion.FechaProximoVencimiento > DateTime.UtcNow
            ? suscripcion.FechaProximoVencimiento
            : null;

        var preapprovalResult = await _pagoRecurrenteGateway.CrearPreapprovalAsync(
            new CrearPreapprovalRequest(
                _plataformaPagoConfiguracion.AccessToken,
                suscripcion.Id,
                $"Suscripción MiTurno - Plan {suscripcion.Plan.Nombre}",
                suscripcion.Plan.Precio,
                suscripcion.Plan.Periodicidad,
                negocio.Email,
                backUrl,
                notificationUrl,
                fechaInicio),
            cancellationToken);

        if (preapprovalResult.IsFailure)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>(preapprovalResult.Error!);

        try
        {
            suscripcion.AsignarPreapproval(preapprovalResult.Value.PreapprovalId);
        }
        catch (DomainException ex)
        {
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>(ex.Message);
        }

        _suscripcionRepository.Update(suscripcion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new IniciarSuscripcionMercadoPagoResponse(preapprovalResult.Value.InitPoint));
    }
}
