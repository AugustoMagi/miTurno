using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones.Dtos;

namespace MiTurno.Application.Features.Suscripciones;

public class ObtenerMiSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;

    public ObtenerMiSuscripcionUseCase(ISuscripcionRepository suscripcionRepository)
    {
        _suscripcionRepository = suscripcionRepository;
    }

    public async Task<Result<MiSuscripcionResponse>> ExecuteAsync(
        Guid negocioId, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure<MiSuscripcionResponse>("Todavía no tenés una suscripción asignada.");

        return Result.Success(suscripcion.ToMiSuscripcionResponse());
    }
}
