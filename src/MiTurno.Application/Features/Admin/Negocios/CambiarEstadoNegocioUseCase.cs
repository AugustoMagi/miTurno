using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.Admin.Negocios;

/// <summary>
/// Activa o desactiva un negocio a criterio del SysAdmin (ej. incumplimiento, abuso, pedido del
/// propio negocio): un negocio inactivo deja de exponer su link público de reservas.
/// </summary>
public class CambiarEstadoNegocioUseCase
{
    private readonly INegocioRepository _negocioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CambiarEstadoNegocioUseCase(INegocioRepository negocioRepository, IUnitOfWork unitOfWork)
    {
        _negocioRepository = negocioRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid negocioId, bool activar, CancellationToken cancellationToken = default)
    {
        var negocio = await _negocioRepository.GetByIdAsync(negocioId, cancellationToken);
        if (negocio is null)
            return Result.Failure("Negocio no encontrado.");

        if (activar)
            negocio.Activar();
        else
            negocio.Desactivar();

        _negocioRepository.Update(negocio);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
