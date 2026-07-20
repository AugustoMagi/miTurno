using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Public.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Reservas;

/// <summary>
/// Registra el rechazo del pago de una reserva (fondos insuficientes, pago cancelado por el
/// cliente, etc.) y libera el turno cancelando la reserva, para que vuelva a quedar disponible.
/// Requiere autenticación (rol Owner/Empleado) por la misma razón que <see cref="ConfirmarPagoUseCase"/>:
/// es una acción sobre el estado del pago, no una consulta del cliente.
/// </summary>
public class RechazarPagoUseCase
{
    private readonly INegocioRepository _negocioRepository;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IReservaRepository _reservaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IEmailNotificador _emailNotificador;
    private readonly IUnitOfWork _unitOfWork;

    public RechazarPagoUseCase(
        INegocioRepository negocioRepository,
        IRecursoRepository recursoRepository,
        IReservaRepository reservaRepository,
        IClienteRepository clienteRepository,
        IEmailNotificador emailNotificador,
        IUnitOfWork unitOfWork)
    {
        _negocioRepository = negocioRepository;
        _recursoRepository = recursoRepository;
        _reservaRepository = reservaRepository;
        _clienteRepository = clienteRepository;
        _emailNotificador = emailNotificador;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReservaResponse>> ExecuteAsync(
        Guid negocioId, Guid reservaId, CancellationToken cancellationToken = default)
    {
        var reserva = await _reservaRepository.GetByIdAsync(reservaId, cancellationToken);
        if (reserva is null)
            return Result.Failure<ReservaResponse>("Reserva no encontrada.");

        var recurso = await _recursoRepository.GetByIdAsync(reserva.RecursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocioId)
            return Result.Failure<ReservaResponse>("Reserva no encontrada.");

        if (reserva.Pago is null)
            return Result.Failure<ReservaResponse>("La reserva no tiene un pago asociado.");

        try
        {
            reserva.Pago.Rechazar();
            reserva.Cancelar();

            _reservaRepository.Update(reserva);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var negocio = await _negocioRepository.GetByIdAsync(negocioId, cancellationToken);
            var cliente = await _clienteRepository.GetByIdAsync(reserva.ClienteId, cancellationToken);
            await _emailNotificador.NotificarReservaRechazadaAsync(
                new NotificacionReserva(
                    cliente!.Email, cliente.Nombre, negocio!.Nombre, recurso.Nombre,
                    reserva.Fecha, reserva.HoraInicio, reserva.HoraFin),
                cancellationToken);

            return Result.Success(new ReservaResponse(
                reserva.Id, reserva.RecursoId, reserva.ClienteId, reserva.Fecha,
                reserva.HoraInicio, reserva.HoraFin, reserva.PrecioTotal, reserva.Estado));
        }
        catch (DomainException ex)
        {
            return Result.Failure<ReservaResponse>(ex.Message);
        }
    }
}
