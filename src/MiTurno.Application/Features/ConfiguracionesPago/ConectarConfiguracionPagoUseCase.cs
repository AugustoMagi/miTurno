using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.ConfiguracionesPago.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.ConfiguracionesPago;

/// <summary>
/// Conecta la cuenta del negocio en un proveedor de pagos. Si ya había una configuración activa
/// (mismo proveedor u otro), la desconecta: solo puede haber una cuenta de cobro activa a la vez,
/// que es de donde IReservaRepository/CrearReservaUseCase asumen que llega el pago del cliente.
/// </summary>
public class ConectarConfiguracionPagoUseCase
{
    private readonly IValidator<ConectarConfiguracionPagoRequest> _validator;
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConectarConfiguracionPagoUseCase(
        IValidator<ConectarConfiguracionPagoRequest> validator,
        IConfiguracionPagoRepository configuracionPagoRepository,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _configuracionPagoRepository = configuracionPagoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ConfiguracionPagoResponse>> ExecuteAsync(
        Guid negocioId, ConectarConfiguracionPagoRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<ConfiguracionPagoResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        try
        {
            var activaAnterior = await _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId, cancellationToken);
            if (activaAnterior is not null)
            {
                activaAnterior.Desconectar();
                _configuracionPagoRepository.Update(activaAnterior);
            }

            var configuracion = ConfiguracionPago.Conectar(negocioId, request.Proveedor, request.Alias);
            await _configuracionPagoRepository.AddAsync(configuracion, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(configuracion.ToResponse());
        }
        catch (DomainException ex)
        {
            return Result.Failure<ConfiguracionPagoResponse>(ex.Message);
        }
    }
}
