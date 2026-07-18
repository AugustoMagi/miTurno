using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Admin.Suscripciones.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Admin.Suscripciones;

/// <summary>Extiende a mano el vencimiento de una Suscripcion (ej. el negocio pagó por fuera de MiTurno).</summary>
public class RenovarSuscripcionManualUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly INegocioRepository _negocioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RenovarSuscripcionManualUseCase(
        ISuscripcionRepository suscripcionRepository, INegocioRepository negocioRepository, IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _negocioRepository = negocioRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SuscripcionAdminResponse>> ExecuteAsync(
        Guid suscripcionId, RenovarSuscripcionManualRequest request, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByIdAsync(suscripcionId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure<SuscripcionAdminResponse>("Suscripción no encontrada.");

        try
        {
            suscripcion.Renovar(request.NuevoVencimiento);
            _suscripcionRepository.Update(suscripcion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var negocio = await _negocioRepository.GetByIdAsync(suscripcion.NegocioId, cancellationToken);
            return Result.Success(suscripcion.ToResponse(negocio!));
        }
        catch (DomainException ex)
        {
            return Result.Failure<SuscripcionAdminResponse>(ex.Message);
        }
    }
}
