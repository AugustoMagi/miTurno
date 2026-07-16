using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.ConfiguracionesPago.Dtos;

namespace MiTurno.Application.Features.ConfiguracionesPago;

/// <summary>Muestra el proveedor de pagos actualmente conectado al negocio del usuario autenticado.</summary>
public class ObtenerConfiguracionPagoUseCase
{
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository;

    public ObtenerConfiguracionPagoUseCase(IConfiguracionPagoRepository configuracionPagoRepository)
    {
        _configuracionPagoRepository = configuracionPagoRepository;
    }

    public async Task<Result<ConfiguracionPagoResponse>> ExecuteAsync(
        Guid negocioId, CancellationToken cancellationToken = default)
    {
        var configuracion = await _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId, cancellationToken);
        if (configuracion is null)
            return Result.Failure<ConfiguracionPagoResponse>("El negocio no tiene un método de pago conectado.");

        return Result.Success(configuracion.ToResponse());
    }
}
