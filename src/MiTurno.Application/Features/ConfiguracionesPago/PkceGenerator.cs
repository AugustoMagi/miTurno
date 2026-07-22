using System.Security.Cryptography;
using System.Text;

namespace MiTurno.Application.Features.ConfiguracionesPago;

/// <summary>Genera el par code_verifier/code_challenge de PKCE (RFC 7636) para el flujo OAuth de Mercado Pago.</summary>
internal static class PkceGenerator
{
    public static string GenerarCodeVerifier() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public static string CalcularCodeChallenge(string codeVerifier) =>
        Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
