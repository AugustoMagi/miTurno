using MiTurno.Domain.Common;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

/// <summary>
/// Dato de cobro del Negocio en un proveedor de pagos (alias/CVU de Mercado Pago, link de pago de
/// Stripe, etc.), para que el cliente le pague directo al dueño y no a una cuenta de MiTurno.
/// Cuando el dueño pega además su propio AccessToken de Mercado Pago, CrearReservaUseCase automatiza
/// el cobro (Checkout Pro + webhook) en vez de depender de que alguien confirme el pago a mano.
/// </summary>
public class ConfiguracionPago : BaseEntity
{
    public Guid NegocioId { get; private set; }
    public ProveedorPago Proveedor { get; private set; }
    public string Alias { get; private set; } = null!;
    public string? AccessToken { get; private set; }
    public bool Activo { get; private set; }

    private ConfiguracionPago() { }

    public static ConfiguracionPago Conectar(Guid negocioId, ProveedorPago proveedor, string alias, string? accessToken = null)
    {
        if (string.IsNullOrWhiteSpace(alias))
            throw new DomainException("El alias o dato de cobro es obligatorio.");

        return new ConfiguracionPago
        {
            NegocioId = negocioId,
            Proveedor = proveedor,
            Alias = alias,
            AccessToken = accessToken,
            Activo = true
        };
    }

    public void Desconectar()
    {
        Activo = false;
        MarcarActualizado();
    }
}
