namespace MiTurno.Application.Common.Models;

/// <summary>Estado normalizado de un pago externo, independiente del proveedor real detrás de IPagoGateway.</summary>
public enum EstadoPagoExterno
{
    Pendiente,
    Aprobado,
    Rechazado
}
