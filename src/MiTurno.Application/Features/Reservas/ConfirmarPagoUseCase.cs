using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Public.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Reservas;

/// <summary>
/// Acredita el pago de una reserva y la confirma automáticamente. Es la confirmación manual que
/// usa el dueño del negocio para transferencias/alias (sin pasarela integrada); los pagos con
/// Mercado Pago se confirman solos vía <see cref="Public.ProcesarNotificacionPagoMercadoPagoUseCase"/>.
/// Requiere autenticación (rol Owner/Empleado) porque marca dinero como cobrado: expuesto sin
/// autenticación, cualquiera con el id de su propia reserva podría auto-confirmarla sin pagar.
/// </summary>
public class ConfirmarPagoUseCase
{
    private readonly INegocioRepository _negocioRepository;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IReservaRepository _reservaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IEmailNotificador _emailNotificador;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmarPagoUseCase(
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
            reserva.Pago.Aprobar();
            reserva.Confirmar();

            _reservaRepository.Update(reserva);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var negocio = await _negocioRepository.GetByIdAsync(negocioId, cancellationToken);
            var cliente = await _clienteRepository.GetByIdAsync(reserva.ClienteId, cancellationToken);
            await _emailNotificador.NotificarReservaConfirmadaAsync(
                new NotificacionReserva(
                    cliente!.Email, cliente.Nombre, negocio!.Nombre, recurso.Nombre,
                    reserva.Fecha, reserva.HoraInicio, reserva.HoraFin),
                cancellationToken);
            await _emailNotificador.NotificarNuevaReservaAlDuenioAsync(
                new NotificacionNuevaReserva(
                    negocio.Email, negocio.Nombre, cliente.Nombre, recurso.Nombre,
                    reserva.Fecha, reserva.HoraInicio, reserva.HoraFin, reserva.PrecioTotal),
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
