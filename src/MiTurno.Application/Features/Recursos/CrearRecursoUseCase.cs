using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Recursos.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Recursos;

/// <summary>Da de alta un recurso (cancha) dentro del negocio del usuario autenticado.</summary>
public class CrearRecursoUseCase
{
    private readonly IValidator<CrearRecursoRequest> _validator;
    private readonly IRecursoRepository _recursoRepository;
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CrearRecursoUseCase(
        IValidator<CrearRecursoRequest> validator,
        IRecursoRepository recursoRepository,
        ISuscripcionRepository suscripcionRepository,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _recursoRepository = recursoRepository;
        _suscripcionRepository = suscripcionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RecursoResponse>> ExecuteAsync(
        Guid negocioId, CrearRecursoRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<RecursoResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        // Un negocio sin Suscripcion asignada (retrocompatibilidad con negocios viejos) no tiene
        // límite de plan que chequear. Se cuentan solo los recursos activos: uno desactivado no
        // ocupa lugar, así que desactivar uno libera espacio para crear otro sin cambiar de plan.
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is not null)
        {
            var recursos = await _recursoRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
            var recursosActivos = recursos.Count(r => r.Activo);
            if (recursosActivos >= suscripcion.Plan.LimiteRecursos)
            {
                var limite = suscripcion.Plan.LimiteRecursos;
                return Result.Failure<RecursoResponse>(
                    $"Alcanzaste el límite de {limite} cancha{(limite == 1 ? "" : "s")} de tu plan actual ({suscripcion.Plan.Nombre}). Cambiá de plan en \"Mi suscripción\" para agregar más.");
            }
        }

        try
        {
            var recurso = Recurso.Crear(
                negocioId, request.Nombre, request.Tipo,
                TimeSpan.FromMinutes(request.DuracionTurnoMinutos), request.Precio);

            await _recursoRepository.AddAsync(recurso, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(recurso.ToResponse());
        }
        catch (DomainException ex)
        {
            return Result.Failure<RecursoResponse>(ex.Message);
        }
    }
}
