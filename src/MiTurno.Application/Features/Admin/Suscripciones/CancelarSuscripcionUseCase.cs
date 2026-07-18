using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.Admin.Suscripciones;

public class CancelarSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarSuscripcionUseCase(ISuscripcionRepository suscripcionRepository, IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid suscripcionId, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByIdAsync(suscripcionId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure("Suscripción no encontrada.");

        suscripcion.Cancelar();
        _suscripcionRepository.Update(suscripcion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
