using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Reservas;

/// <summary>
/// Cancela una reserva, verificando que su recurso pertenezca al negocio del usuario autenticado.
/// No toca el Pago asociado: el reembolso, si corresponde, lo maneja la integración con la
/// pasarela de pago. Le avisa al cliente por email que su turno fue cancelado por el negocio.
/// </summary>
public class CancelarReservaUseCase
{
    private readonly IReservaRepository _reservaRepository;
    private readonly IRecursoRepository _recursoRepository;
    private readonly INegocioRepository _negocioRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IEmailNotificador _emailNotificador;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarReservaUseCase(
        IReservaRepository reservaRepository,
        IRecursoRepository recursoRepository,
        INegocioRepository negocioRepository,
        IClienteRepository clienteRepository,
        IEmailNotificador emailNotificador,
        IUnitOfWork unitOfWork)
    {
        _reservaRepository = reservaRepository;
        _recursoRepository = recursoRepository;
        _negocioRepository = negocioRepository;
        _clienteRepository = clienteRepository;
        _emailNotificador = emailNotificador;
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

            var negocio = await _negocioRepository.GetByIdAsync(negocioId, cancellationToken);
            var cliente = await _clienteRepository.GetByIdAsync(reserva.ClienteId, cancellationToken);
            await _emailNotificador.NotificarReservaCanceladaAsync(
                new NotificacionReserva(
                    cliente!.Email, cliente.Nombre, negocio!.Nombre, recurso.Nombre,
                    reserva.Fecha, reserva.HoraInicio, reserva.HoraFin),
                cancellationToken);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
