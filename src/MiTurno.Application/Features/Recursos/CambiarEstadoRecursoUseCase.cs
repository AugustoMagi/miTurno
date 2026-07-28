using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Common.Services;

namespace MiTurno.Application.Features.Recursos;

/// <summary>
/// Activa o desactiva un recurso (baja lógica, ya que puede tener reservas asociadas),
/// verificando que pertenezca al negocio del usuario autenticado. Reactivar uno cuenta como sumar
/// una cancha activa más, así que pasa por el mismo límite de plan que crear un recurso nuevo — si
/// no, se podría rodear el límite desactivando y reactivando en vez de creando.
/// </summary>
public class CambiarEstadoRecursoUseCase
{
    private readonly IRecursoRepository _recursoRepository;
    private readonly ValidarLimiteRecursosService _validarLimiteRecursosService;
    private readonly IUnitOfWork _unitOfWork;

    public CambiarEstadoRecursoUseCase(
        IRecursoRepository recursoRepository,
        ValidarLimiteRecursosService validarLimiteRecursosService,
        IUnitOfWork unitOfWork)
    {
        _recursoRepository = recursoRepository;
        _validarLimiteRecursosService = validarLimiteRecursosService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(
        Guid negocioId, Guid recursoId, bool activar, CancellationToken cancellationToken = default)
    {
        var recurso = await _recursoRepository.GetByIdAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocioId)
            return Result.Failure("Recurso no encontrado.");

        if (activar && !recurso.Activo)
        {
            var limiteResult = await _validarLimiteRecursosService.ValidarAsync(negocioId, cancellationToken);
            if (limiteResult.IsFailure)
                return limiteResult;
        }

        if (activar)
            recurso.Activar();
        else
            recurso.Desactivar();

        _recursoRepository.Update(recurso);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
