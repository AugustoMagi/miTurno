using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Recursos.Horarios;

/// <summary>Elimina una franja horaria de un recurso, verificando que pertenezca al negocio autenticado.</summary>
public class EliminarHorarioDisponibleUseCase
{
    private readonly IRecursoRepository _recursoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EliminarHorarioDisponibleUseCase(IRecursoRepository recursoRepository, IUnitOfWork unitOfWork)
    {
        _recursoRepository = recursoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(
        Guid negocioId, Guid recursoId, Guid horarioId, CancellationToken cancellationToken = default)
    {
        var recurso = await _recursoRepository.GetConHorariosYBloqueosAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocioId)
            return Result.Failure("Recurso no encontrado.");

        try
        {
            recurso.EliminarHorarioDisponible(horarioId);

            _recursoRepository.Update(recurso);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
