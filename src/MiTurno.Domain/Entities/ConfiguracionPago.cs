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

    /// <summary>
    /// Solo presente cuando la conexión vino del flujo OAuth (Conectar con Mercado Pago): permite
    /// renovar el AccessToken sin volver a pedirle autorización al negocio. Las conexiones donde el
    /// negocio pegó su AccessToken a mano no tienen RefreshToken y ese token no vence.
    /// </summary>
    public string? RefreshToken { get; private set; }

    /// <summary>Vencimiento del AccessToken obtenido por OAuth. Null si no aplica (token manual, sin vencimiento conocido).</summary>
    public DateTime? AccessTokenExpiraEn { get; private set; }

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

    /// <summary>Conecta Mercado Pago vía OAuth: el negocio autorizó a MiTurno y esto guarda los tokens que Mercado Pago entregó.</summary>
    public static ConfiguracionPago ConectarConOAuth(
        Guid negocioId, string alias, string accessToken, string refreshToken, DateTime accessTokenExpiraEn)
    {
        if (string.IsNullOrWhiteSpace(alias))
            throw new DomainException("El alias o dato de cobro es obligatorio.");
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new DomainException("El Access Token es obligatorio.");
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new DomainException("El Refresh Token es obligatorio.");

        return new ConfiguracionPago
        {
            NegocioId = negocioId,
            Proveedor = ProveedorPago.MercadoPago,
            Alias = alias,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiraEn = accessTokenExpiraEn,
            Activo = true
        };
    }

    /// <summary>Reemplaza el AccessToken/RefreshToken tras renovarlos contra Mercado Pago (el refresh_token también rota en cada uso).</summary>
    public void ActualizarTokensOAuth(string accessToken, string refreshToken, DateTime accessTokenExpiraEn)
    {
        if (RefreshToken is null)
            throw new DomainException("Esta configuración no fue conectada por OAuth.");

        AccessToken = accessToken;
        RefreshToken = refreshToken;
        AccessTokenExpiraEn = accessTokenExpiraEn;
        MarcarActualizado();
    }

    public void Desconectar()
    {
        Activo = false;
        MarcarActualizado();
    }
}
