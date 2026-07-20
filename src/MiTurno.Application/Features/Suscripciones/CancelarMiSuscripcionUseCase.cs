using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.Suscripciones;

public class CancelarMiSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarMiSuscripcionUseCase(ISuscripcionRepository suscripcionRepository, IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid negocioId, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure("Todavía no tenés una suscripción asignada.");

        suscripcion.Cancelar();
        _suscripcionRepository.Update(suscripcion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
