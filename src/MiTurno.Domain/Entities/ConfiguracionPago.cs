using MiTurno.Domain.Common;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

/// <summary>
/// Vincula la cuenta del proveedor de pagos (Mercado Pago, Stripe) del propio Negocio,
/// de forma que el pago de una Reserva le llegue directo al dueño y no a una cuenta de MiTurno.
/// </summary>
public class ConfiguracionPago : BaseEntity
{
    public Guid NegocioId { get; private set; }
    public ProveedorPago Proveedor { get; private set; }
    public string CredencialesOAuth { get; private set; } = null!;
    public bool Activo { get; private set; }

    private ConfiguracionPago() { }

    public static ConfiguracionPago Conectar(Guid negocioId, ProveedorPago proveedor, string credencialesOAuth)
    {
        if (string.IsNullOrWhiteSpace(credencialesOAuth))
            throw new DomainException("Las credenciales OAuth son obligatorias.");

        return new ConfiguracionPago
        {
            NegocioId = negocioId,
            Proveedor = proveedor,
            CredencialesOAuth = credencialesOAuth,
            Activo = true
        };
    }

    public void Desconectar()
    {
        Activo = false;
        MarcarActualizado();
    }
}
