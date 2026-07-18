namespace MiTurno.Application.Common.Models;

/// <summary>Estado de un pago consultado directamente en la pasarela externa, por su id.</summary>
public record EstadoPagoExternoResult(string PagoExternoId, EstadoPagoExterno Estado, string? ExternalReference);
