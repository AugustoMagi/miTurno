using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Public.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Public;

/// <summary>
/// Crea una reserva a partir de un turno elegido por el cliente. Vuelve a validar la
/// disponibilidad contra el estado actual (no confía en el listado previo, que puede
/// haber quedado desactualizado por otra reserva concurrente) y deja un Pago pendiente
/// asociado, a la espera de la confirmación de la pasarela de pago.
/// </summary>
public class CrearReservaUseCase
{
    private readonly IValidator<CrearReservaRequest> _validator;
    private readonly INegocioRepository _negocioRepository;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IReservaRepository _reservaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository;
    private readonly IPagoGateway _pagoGateway;
    private readonly IUnitOfWork _unitOfWork;

    public CrearReservaUseCase(
        IValidator<CrearReservaRequest> validator,
        INegocioRepository negocioRepository,
        IRecursoRepository recursoRepository,
        IReservaRepository reservaRepository,
        IClienteRepository clienteRepository,
        IConfiguracionPagoRepository configuracionPagoRepository,
        IPagoGateway pagoGateway,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _negocioRepository = negocioRepository;
        _recursoRepository = recursoRepository;
        _reservaRepository = reservaRepository;
        _clienteRepository = clienteRepository;
        _configuracionPagoRepository = configuracionPagoRepository;
        _pagoGateway = pagoGateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReservaResponse>> ExecuteAsync(
        string slug, Guid recursoId, CrearReservaRequest request, string webhookBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<ReservaResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        if (request.Fecha < DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Failure<ReservaResponse>("No se pueden reservar turnos en fechas pasadas.");

        var negocio = await _negocioRepository.GetBySlugAsync(slug, cancellationToken);
        if (negocio is null || !negocio.Activo)
            return Result.Failure<ReservaResponse>("Negocio no encontrado.");

        var recurso = await _recursoRepository.GetConHorariosYBloqueosAsync(recursoId, cancellationToken);
        if (recurso is null || recurso.NegocioId != negocio.Id || !recurso.Activo)
            return Result.Failure<ReservaResponse>("Recurso no encontrado.");

        var horaFin = request.HoraInicio + recurso.DuracionTurno;

        if (recurso.BloqueosFecha.Any(b => b.Fecha == request.Fecha))
            return Result.Failure<ReservaResponse>("El recurso no tiene disponibilidad ese día.");

        var horarioValido = recurso.HorariosDisponibles.Any(h =>
            h.DiaSemana == request.Fecha.DayOfWeek &&
            h.HoraInicio <= request.HoraInicio &&
            horaFin <= h.HoraFin);
        if (!horarioValido)
            return Result.Failure<ReservaResponse>("El horario seleccionado no está disponible.");

        var reservasDelDia = await _reservaRepository.GetByRecursoYFechaAsync(recursoId, request.Fecha, cancellationToken);
        var seSuperpone = reservasDelDia
            .Where(r => r.Estado != EstadoReserva.Cancelada)
            .Any(r => r.HoraInicio < horaFin && request.HoraInicio < r.HoraFin);
        if (seSuperpone)
            return Result.Failure<ReservaResponse>("El turno seleccionado ya fue reservado.");

        try
        {
            var cliente = await _clienteRepository.GetByEmailAsync(request.ClienteEmail, cancellationToken);
            if (cliente is null)
            {
                cliente = Cliente.Crear(request.ClienteNombre, request.ClienteEmail, request.ClienteTelefono);
                await _clienteRepository.AddAsync(cliente, cancellationToken);
            }
            else
            {
                cliente.ActualizarDatosContacto(request.ClienteNombre, request.ClienteTelefono);
                _clienteRepository.Update(cliente);
            }

            var reserva = Reserva.Crear(recursoId, cliente.Id, request.Fecha, request.HoraInicio, horaFin, recurso.Precio);
            var pago = Pago.Registrar(reserva.Id, recurso.Precio);
            reserva.AsignarPago(pago);

            await _reservaRepository.AddAsync(reserva, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var linkPago = await CrearLinkPagoAsync(negocio.Id, slug, recurso, reserva, webhookBaseUrl, cancellationToken);

            return Result.Success(new ReservaResponse(
                reserva.Id, reserva.RecursoId, reserva.ClienteId, reserva.Fecha,
                reserva.HoraInicio, reserva.HoraFin, reserva.PrecioTotal, reserva.Estado, linkPago));
        }
        catch (DomainException ex)
        {
            return Result.Failure<ReservaResponse>(ex.Message);
        }
    }

    /// <summary>
    /// Si el negocio tiene Mercado Pago conectado con un AccessToken, crea la preferencia de Checkout
    /// Pro y devuelve su link. Es best-effort: si la pasarela falla (caída, token inválido), la
    /// reserva ya quedó creada igual y el dueño sigue el flujo manual de confirmar el pago a mano.
    /// </summary>
    private async Task<string?> CrearLinkPagoAsync(
        Guid negocioId, string slug, Recurso recurso, Reserva reserva, string webhookBaseUrl,
        CancellationToken cancellationToken)
    {
        var configuracionPago = await _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId, cancellationToken);
        if (configuracionPago is not { Proveedor: ProveedorPago.MercadoPago, AccessToken: not null })
            return null;

        var notificationUrl =
            $"{webhookBaseUrl}/api/public/negocios/{slug}/reservas/{reserva.Id}/pago/webhook/mercadopago";

        var preferenciaResult = await _pagoGateway.CrearPreferenciaAsync(
            new CrearPreferenciaPagoRequest(
                configuracionPago.AccessToken, reserva.Id, $"Turno en {recurso.Nombre}", reserva.PrecioTotal, notificationUrl),
            cancellationToken);

        return preferenciaResult.IsSuccess ? preferenciaResult.Value.LinkPago : null;
    }
}
