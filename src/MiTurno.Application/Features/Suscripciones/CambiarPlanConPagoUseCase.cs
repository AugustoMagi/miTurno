using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Suscripciones.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Suscripciones;

/// <summary>
/// Cambia a un plan pago: a diferencia de CambiarPlanMiSuscripcionUseCase (que cambia el plan al
/// toque), acá el plan y la Preapproval vigentes no se tocan hasta que ObtenerMiSuscripcionUseCase
/// confirme que el negocio efectivamente autorizó el pago del plan nuevo — si entra a Mercado Pago y
/// no paga (o vuelve atrás), no pierde el plan ni el cobro automático que ya tenía funcionando.
/// </summary>
public class CambiarPlanConPagoUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly INegocioRepository _negocioRepository;
    private readonly ValidarLimiteRecursosService _validarLimiteRecursosService;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway;
    private readonly IFrontendConfiguracion _frontendConfiguracion;
    private readonly IUnitOfWork _unitOfWork;

    public CambiarPlanConPagoUseCase(
        ISuscripcionRepository suscripcionRepository,
        IPlanRepository planRepository,
        INegocioRepository negocioRepository,
        ValidarLimiteRecursosService validarLimiteRecursosService,
        IPlataformaPagoConfiguracion plataformaPagoConfiguracion,
        IPagoRecurrenteGateway pagoRecurrenteGateway,
        IFrontendConfiguracion frontendConfiguracion,
        IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _planRepository = planRepository;
        _negocioRepository = negocioRepository;
        _validarLimiteRecursosService = validarLimiteRecursosService;
        _plataformaPagoConfiguracion = plataformaPagoConfiguracion;
        _pagoRecurrenteGateway = pagoRecurrenteGateway;
        _frontendConfiguracion = frontendConfiguracion;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IniciarSuscripcionMercadoPagoResponse>> ExecuteAsync(
        Guid negocioId, Guid nuevoPlanId, string webhookBaseUrl, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Todavía no tenés una suscripción asignada.");

        var nuevoPlan = await _planRepository.GetByIdAsync(nuevoPlanId, cancellationToken);
        if (nuevoPlan is null || !nuevoPlan.Activo)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Plan no encontrado.");

        if (nuevoPlan.Precio <= 0)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Este plan no requiere cobro.");

        var limiteResult = await _validarLimiteRecursosService.ValidarCambioDePlanAsync(
            negocioId, nuevoPlan, cancellationToken);
        if (limiteResult.IsFailure)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>(limiteResult.Error!);

        var negocio = await _negocioRepository.GetByIdAsync(negocioId, cancellationToken);
        if (negocio is null)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>("Negocio no encontrado.");

        var notificationUrl = $"{webhookBaseUrl}/api/public/suscripciones/{suscripcion.Id}/webhook/recurrente";
        var backUrl = $"{_frontendConfiguracion.BaseUrl}/panel/suscripcion?mp=vuelta";

        // Cambiar de plan es un compromiso nuevo a un precio nuevo: se cobra ya (sin start_date), no
        // se espera al vencimiento del plan viejo.
        var preapprovalResult = await _pagoRecurrenteGateway.CrearPreapprovalAsync(
            new CrearPreapprovalRequest(
                _plataformaPagoConfiguracion.AccessToken,
                suscripcion.Id,
                $"Suscripción MiTurno - Plan {nuevoPlan.Nombre}",
                nuevoPlan.Precio,
                nuevoPlan.Periodicidad,
                negocio.Email,
                backUrl,
                notificationUrl,
                FechaInicio: null),
            cancellationToken);

        if (preapprovalResult.IsFailure)
            return Result.Failure<IniciarSuscripcionMercadoPagoResponse>(preapprovalResult.Error!);

        try
        {
            suscripcion.IniciarCambioDePlanConPago(nuevoPlan.Id, preapprovalResult.Value.PreapprovalId);
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
