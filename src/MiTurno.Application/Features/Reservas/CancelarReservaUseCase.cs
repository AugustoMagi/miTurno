using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Reservas;

/// <summary>
/// Cancela una reserva, verificando que su recurso pertenezca al negocio del usuario autenticado.
/// No toca el Pago asociado: el reembolso, si corresponde, lo maneja la integración con la
/// pasarela de pago.
/// </summary>
public class CancelarReservaUseCase
{
    private readonly IReservaRepository _reservaRepository;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarReservaUseCase(
        IReservaRepository reservaRepository,
        IRecursoRepository recursoRepository,
        IUnitOfWork unitOfWork)
    {
        _reservaRepository = reservaRepository;
        _recursoRepository = recursoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(
        Guid negocioId, Guid reservaId, CancellationToken cancellationToken = default)
    {
        var reserva = await _reservaRepository.GetByIdAsync(reservaId, cancellationToken);
        if (reserva is null)
            return Result.Failure("Reserva no encontrada.");

        var recurso = await _recursoRepository.GetByIdAsync(reserva.RecursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocioId)
            return Result.Failure("Reserva no encontrada.");

        try
        {
            reserva.Cancelar();

            _reservaRepository.Update(reserva);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
