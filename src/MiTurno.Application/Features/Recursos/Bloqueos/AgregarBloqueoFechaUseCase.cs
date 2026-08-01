using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Recursos.Bloqueos.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Recursos.Bloqueos;

/// <summary>
/// Agrega un bloqueo de fecha (día completo) u horario (un rango puntual dentro del día) a un
/// recurso, verificando que pertenezca al negocio autenticado. El bloqueo no cancela las reservas
/// activas que ya existan en ese rango (eso queda a criterio del dueño, vía CancelarReservaUseCase),
/// pero se informan en la respuesta para que no pasen desapercibidas.
/// </summary>
public class AgregarBloqueoFechaUseCase
{
    private readonly IValidator<AgregarBloqueoFechaRequest> _validator;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IReservaRepository _reservaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AgregarBloqueoFechaUseCase(
        IValidator<AgregarBloqueoFechaRequest> validator,
        IRecursoRepository recursoRepository,
        IReservaRepository reservaRepository,
        IClienteRepository clienteRepository,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _recursoRepository = recursoRepository;
        _reservaRepository = reservaRepository;
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BloqueoFechaResponse>> ExecuteAsync(
        Guid negocioId, Guid recursoId, AgregarBloqueoFechaRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<BloqueoFechaResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var recurso = await _recursoRepository.GetConHorariosYBloqueosAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocioId)
            return Result.Failure<BloqueoFechaResponse>("Recurso no encontrado.");

        try
        {
            var bloqueo = BloqueoFecha.Crear(
                recursoId, request.Fecha, request.HoraInicio, request.HoraFin, request.Motivo);
            recurso.AgregarBloqueoFecha(bloqueo);

            _recursoRepository.Update(recurso);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var reservasAfectadas = await ObtenerReservasAfectadasAsync(recursoId, bloqueo, cancellationToken);

            return Result.Success(bloqueo.ToResponse(reservasAfectadas));
        }
        catch (DomainException ex)
        {
            return Result.Failure<BloqueoFechaResponse>(ex.Message);
        }
    }

    private async Task<IReadOnlyList<ReservaAfectadaResponse>> ObtenerReservasAfectadasAsync(
        Guid recursoId, BloqueoFecha bloqueo, CancellationToken cancellationToken)
    {
        var reservasDelDia = await _reservaRepository.GetByRecursoYFechaAsync(recursoId, bloqueo.Fecha, cancellationToken);
        var reservasActivas = reservasDelDia
            .Where(r => r.Estado is EstadoReserva.Pendiente or EstadoReserva.Confirmada)
            .Where(r => bloqueo.Cubre(r.HoraInicio, r.HoraFin))
            .ToList();

        if (reservasActivas.Count == 0)
            return [];

        var clientesPorId = new Dictionary<Guid, Cliente>();
        foreach (var clienteId in reservasActivas.Select(r => r.ClienteId).Distinct())
        {
            var cliente = await _clienteRepository.GetByIdAsync(clienteId, cancellationToken);
            clientesPorId[clienteId] = cliente!;
        }

        return reservasActivas
            .Select(r =>
            {
                var cliente = clientesPorId[r.ClienteId];
                return new ReservaAfectadaResponse(
                    r.Id, r.ClienteId, cliente.Nombre, cliente.Email, r.HoraInicio, r.HoraFin, r.Estado);
            })
            .ToList();
    }
}
