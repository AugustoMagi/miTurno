using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.Recursos;

/// <summary>
/// Activa o desactiva un recurso (baja lógica, ya que puede tener reservas asociadas),
/// verificando que pertenezca al negocio del usuario autenticado.
/// </summary>
public class CambiarEstadoRecursoUseCase
{
    private readonly IRecursoRepository _recursoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CambiarEstadoRecursoUseCase(IRecursoRepository recursoRepository, IUnitOfWork unitOfWork)
    {
        _recursoRepository = recursoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(
        Guid negocioId, Guid recursoId, bool activar, CancellationToken cancellationToken = default)
    {
        var recurso = await _recursoRepository.GetByIdAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocioId)
            return Result.Failure("Recurso no encontrado.");

        if (activar)
            recurso.Activar();
        else
            recurso.Desactivar();

        _recursoRepository.Update(recurso);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
