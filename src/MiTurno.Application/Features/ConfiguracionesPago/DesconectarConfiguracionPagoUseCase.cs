using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.ConfiguracionesPago;

/// <summary>Desconecta el proveedor de pagos activo del negocio del usuario autenticado.</summary>
public class DesconectarConfiguracionPagoUseCase
{
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DesconectarConfiguracionPagoUseCase(
        IConfiguracionPagoRepository configuracionPagoRepository, IUnitOfWork unitOfWork)
    {
        _configuracionPagoRepository = configuracionPagoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid negocioId, CancellationToken cancellationToken = default)
    {
        var configuracion = await _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId, cancellationToken);
        if (configuracion is null)
            return Result.Failure("El negocio no tiene un método de pago conectado.");

        configuracion.Desconectar();

        _configuracionPagoRepository.Update(configuracion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
