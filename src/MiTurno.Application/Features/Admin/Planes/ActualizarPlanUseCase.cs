using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Admin.Planes.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Admin.Planes;

public class ActualizarPlanUseCase
{
    private readonly IValidator<ActualizarPlanRequest> _validator;
    private readonly IPlanRepository _planRepository;
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway;
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarPlanUseCase(
        IValidator<ActualizarPlanRequest> validator,
        IPlanRepository planRepository,
        ISuscripcionRepository suscripcionRepository,
        IPlataformaPagoConfiguracion plataformaPagoConfiguracion,
        IPagoRecurrenteGateway pagoRecurrenteGateway,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _planRepository = planRepository;
        _suscripcionRepository = suscripcionRepository;
        _plataformaPagoConfiguracion = plataformaPagoConfiguracion;
        _pagoRecurrenteGateway = pagoRecurrenteGateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlanResponse>> ExecuteAsync(
        Guid planId, ActualizarPlanRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<PlanResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
            return Result.Failure<PlanResponse>("Plan no encontrado.");

        var precioAnterior = plan.Precio;

        try
        {
            plan.Actualizar(
                request.Nombre, request.Precio, request.Periodicidad,
                request.LimiteRecursos, request.LimiteReservasPorMes);

            _planRepository.Update(plan);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Best-effort: si cambió el precio, se sincroniza el monto en cada Preapproval de Mercado
            // Pago que ya esté cobrando este plan, para que el próximo cobro automático refleje el
            // nuevo precio. Si alguna falla (caída puntual de MP), el precio del plan ya quedó
            // guardado igual; esa suscripción puntual seguirá cobrando el monto viejo hasta que se
            // reintente (p. ej. reeditando el plan).
            if (plan.Precio != precioAnterior)
            {
                var suscripciones = await _suscripcionRepository.GetConPreapprovalPorPlanIdAsync(plan.Id, cancellationToken);
                foreach (var suscripcion in suscripciones)
                {
                    await _pagoRecurrenteGateway.ActualizarMontoPreapprovalAsync(
                        _plataformaPagoConfiguracion.AccessToken, suscripcion.MercadoPagoPreapprovalId!,
                        plan.Precio, cancellationToken);
                }
            }

            return Result.Success(plan.ToResponse());
        }
        catch (DomainException ex)
        {
            return Result.Failure<PlanResponse>(ex.Message);
        }
    }
}
