using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Suscripciones;

/// <summary>
/// Genera (o reusa, si ya había uno pendiente) el pago de la suscripción SaaS del negocio y su
/// preferencia de Checkout Pro, cobrada con la cuenta de Mercado Pago de la propia plataforma
/// MiTurno (no la del negocio).
/// </summary>
public class GenerarPagoSuscripcionUseCase
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion;
    private readonly IPagoGateway _pagoGateway;
    private readonly IUnitOfWork _unitOfWork;

    public GenerarPagoSuscripcionUseCase(
        ISuscripcionRepository suscripcionRepository,
        IPlataformaPagoConfiguracion plataformaPagoConfiguracion,
        IPagoGateway pagoGateway,
        IUnitOfWork unitOfWork)
    {
        _suscripcionRepository = suscripcionRepository;
        _plataformaPagoConfiguracion = plataformaPagoConfiguracion;
        _pagoGateway = pagoGateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagoSuscripcionResponse>> ExecuteAsync(
        Guid negocioId, string webhookBaseUrl, CancellationToken cancellationToken = default)
    {
        var suscripcion = await _suscripcionRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (suscripcion is null)
            return Result.Failure<PagoSuscripcionResponse>("Todavía no tenés una suscripción asignada.");

        var pagoPendiente = suscripcion.Pagos.FirstOrDefault(p => p.Estado == EstadoPago.Pendiente);
        if (pagoPendiente is null)
        {
            try
            {
                pagoPendiente = PagoSuscripcion.Registrar(suscripcion.Id, suscripcion.Plan.Precio, transaccionExternalId: null);
            }
            catch (DomainException ex)
            {
                return Result.Failure<PagoSuscripcionResponse>(ex.Message);
            }

            suscripcion.RegistrarPago(pagoPendiente);
            _suscripcionRepository.Update(suscripcion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var notificationUrl =
            $"{webhookBaseUrl}/api/public/suscripciones/{suscripcion.Id}/pago/webhook/mercadopago";

        var preferenciaResult = await _pagoGateway.CrearPreferenciaAsync(
            new CrearPreferenciaPagoRequest(
                _plataformaPagoConfiguracion.AccessToken, pagoPendiente.Id,
                $"Suscripción MiTurno - Plan {suscripcion.Plan.Nombre}", pagoPendiente.Monto, notificationUrl),
            cancellationToken);

        var linkPago = preferenciaResult.IsSuccess ? preferenciaResult.Value.LinkPago : null;

        return Result.Success(new PagoSuscripcionResponse(
            pagoPendiente.Id, pagoPendiente.Monto, pagoPendiente.Estado, linkPago));
    }
}
