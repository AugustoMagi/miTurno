using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Admin.Suscripciones.Dtos;

namespace MiTurno.Application.Features.Admin.Suscripciones;

public class CambiarPlanSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly INegocioRepository _negocioRepository;
    private readonly ValidarLimiteRecursosService _validarLimiteRecursosService;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway;
    private readonly IUnitOfWork _unitOfWork;

    public CambiarPlanSuscripcionUseCase(
        ISuscripcionRepository suscripcionRepository,
        IPlanRepository planRepository,
        INegocioRepository negocioRepository,
        ValidarLimiteRecursosService validarLimiteRecursosService,
        IPlataformaPagoConfiguracion plataformaPagoConfiguracion,
        IPagoRecurrenteGateway pagoRecurrenteGateway,
        IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _planRepository = planRepository;
        _negocioRepository = negocioRepository;
        _validarLimiteRecursosService = validarLimiteRecursosService;
        _plataformaPagoConfiguracion = plataformaPagoConfiguracion;
        _pagoRecurrenteGateway = pagoRecurrenteGateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SuscripcionAdminResponse>> ExecuteAsync(
        Guid suscripcionId, CambiarPlanSuscripcionRequest request, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByIdAsync(suscripcionId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure<SuscripcionAdminResponse>("Suscripción no encontrada.");

        var nuevoPlan = await _planRepository.GetByIdAsync(request.NuevoPlanId, cancellationToken);
        if (nuevoPlan is null)
            return Result.Failure<SuscripcionAdminResponse>("Plan no encontrado.");

        var limiteResult = await _validarLimiteRecursosService.ValidarCambioDePlanAsync(
            suscripcion.NegocioId, nuevoPlan, cancellationToken);
        if (limiteResult.IsFailure)
            return Result.Failure<SuscripcionAdminResponse>(limiteResult.Error!);

        suscripcion.CambiarPlan(nuevoPlan);
        _suscripcionRepository.Update(suscripcion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (suscripcion.MercadoPagoPreapprovalId is not null)
        {
            await _pagoRecurrenteGateway.ActualizarMontoPreapprovalAsync(
                _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId,
                nuevoPlan.Precio, cancellationToken);
        }

        var negocio = await _negocioRepository.GetByIdAsync(suscripcion.NegocioId, cancellationToken);
        return Result.Success(suscripcion.ToResponse(negocio!));
    }
}
