using MiTurno.Domain.Enums;

namespace MiTurno.Application.Features.Suscripciones.Dtos;

public record PagoSuscripcionResponse(Guid Id, decimal Monto, EstadoPago Estado, string? LinkPago);
