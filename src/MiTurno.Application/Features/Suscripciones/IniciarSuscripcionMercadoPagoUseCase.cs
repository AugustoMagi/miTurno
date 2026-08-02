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

    public async Task<Result<IniciarSuscripcionMercadoPagoResponse>> ExecuteAsync(
        Guid negocioId, string webhookBaseUrl, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Todavía no tenés una suscripción asignada.");

        if (suscripcion.Plan.Precio <= 0)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Este plan no requiere cobro.");

        if (suscripcion.MercadoPagoPreapprovalId is not null)
        {
            // Un Preapproval que el negocio nunca autorizó (o canceló desde su propia cuenta de MP)
            // puede quedar cancelado del lado de Mercado Pago sin que el webhook nos avise: en vez
            // de bloquear para siempre, reconsultamos y si ya está muerto lo soltamos y seguimos.
            var estadoResult = await _pagoRecurrenteGateway.ObtenerPreapprovalAsync(
                _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId, cancellationToken);

            if (!estadoResult.IsSuccess || estadoResult.Value.Status != "cancelled")
                return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Ya tenés el cobro automático de Mercado Pago activado.");

            suscripcion.QuitarPreapproval();
        }

        var negocio = await _negocioRepository.GetByIdAsync(negocioId, cancellationToken);
        if (negocio is null)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Negocio no encontrado.");

        var notificationUrl = $"{webhookBaseUrl}/api/public/suscripciones/{suscripcion.Id}/webhook/recurrente";
        var backUrl = $"{_frontendConfiguracion.BaseUrl}/panel/suscripcion?mp=vuelta";

        // El primer cobro no debe salir el día que se autoriza la Preapproval: si ya hay un período
        // vigente (de prueba, o pago pero con el cobro automático apagado), Mercado Pago no debe
        // cobrar hasta que ese período termine. Sólo si ya venció se cobra de inmediato.
        var ahora = DateTime.UtcNow;
        var fechaInicio = suscripcion.FechaProximoVencimiento > ahora ? suscripcion.FechaProximoVencimiento : ahora;

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
