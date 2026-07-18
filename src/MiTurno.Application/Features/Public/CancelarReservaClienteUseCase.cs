using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Public.Dtos;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Public;

/// <summary>
/// Cancela una reserva a pedido del propio cliente, identificada solo por el slug del negocio y
/// el id de la reserva (sin autenticación, igual que el resto del flujo público). No toca el Pago
/// asociado: el reembolso, si corresponde, lo maneja la integración con la pasarela de pago. Le
/// avisa por email al dueño del negocio para que sepa que el horario volvió a estar disponible.
/// </summary>
public class CancelarReservaClienteUseCase
{
    private readonly INegocioRepository _negocioRepository;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IReservaRepository _reservaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IEmailNotificador _emailNotificador;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarReservaClienteUseCase(
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
        string slug, Guid reservaId, CancellationToken cancellationToken = default)
    {
        var negocio = await _negocioRepository.GetBySlugAsync(slug, cancellationToken);
        if (negocio is null || !negocio.Activo)
            return Result.Failure<ReservaResponse>("Negocio no encontrado.");

        var reserva = await _reservaRepository.GetByIdAsync(reservaId, cancellationToken);
        if (reserva is null)
            return Result.Failure<ReservaResponse>("Reserva no encontrada.");

        var recurso = await _recursoRepository.GetByIdAsync(reserva.RecursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocio.Id)
            return Result.Failure<ReservaResponse>("Reserva no encontrada.");

        try
        {
            reserva.Cancelar();

            _reservaRepository.Update(reserva);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var cliente = await _clienteRepository.GetByIdAsync(reserva.ClienteId, cancellationToken);
            await _emailNotificador.NotificarReservaCanceladaPorClienteAsync(
                new NotificacionNuevaReserva(
                    negocio.Email, negocio.Nombre, cliente!.Nombre, recurso.Nombre,
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
