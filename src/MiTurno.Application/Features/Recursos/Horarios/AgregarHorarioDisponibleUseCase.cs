using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Recursos.Horarios.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Recursos.Horarios;

/// <summary>Agrega una franja horaria semanal a un recurso, verificando que pertenezca al negocio autenticado.</summary>
public class AgregarHorarioDisponibleUseCase
{
    private readonly IValidator<AgregarHorarioDisponibleRequest> _validator;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AgregarHorarioDisponibleUseCase(
        IValidator<AgregarHorarioDisponibleRequest> validator,
        IRecursoRepository recursoRepository,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _recursoRepository = recursoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<HorarioDisponibleResponse>> ExecuteAsync(
        Guid negocioId, Guid recursoId, AgregarHorarioDisponibleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<HorarioDisponibleResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var recurso = await _recursoRepository.GetConHorariosYBloqueosAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocioId)
            return Result.Failure<HorarioDisponibleResponse>("Recurso no encontrado.");

        try
        {
            var horario = HorarioDisponible.Crear(recursoId, request.DiaSemana, request.HoraInicio, request.HoraFin);
            recurso.AgregarHorarioDisponible(horario);

            _recursoRepository.Update(recurso);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(horario.ToResponse());
        }
        catch (DomainException ex)
        {
            return Result.Failure<HorarioDisponibleResponse>(ex.Message);
        }
    }
}
