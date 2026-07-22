namespace MiTurno.Application.Common.Models;

/// <summary>Tokens que Mercado Pago entrega al canjear el "code" de autorización o al refrescar.</summary>
public record MercadoPagoOAuthTokenResult(string AccessToken, string RefreshToken, int ExpiraEnSegundos);

/// <summary>Datos mínimos del usuario de Mercado Pago conectado, para mostrarle al negocio qué cuenta quedó vinculada.</summary>
public record MercadoPagoUsuarioResult(string? Email, string? Nickname);

/// <summary>Contenido del "state" OAuth una vez descifrado: a qué negocio pertenece el intento y el code_verifier de PKCE.</summary>
public record EstadoOAuth(Guid NegocioId, string CodeVerifier);
