using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Common.Interfaces;

/// <summary>
/// Empaqueta negocioId + code_verifier (PKCE) en un "state" opaco y firmado para el flujo OAuth de
/// Mercado Pago, sin necesitar guardar nada en base de datos entre el paso de autorización y el
/// callback: el propio state, cifrado, viaja y vuelve a través de Mercado Pago. Expira solo, así que
/// un state viejo o alterado nunca es válido.
/// </summary>
public interface IEstadoOAuthProtector
{
    string Proteger(Guid negocioId, string codeVerifier);

    Result<EstadoOAuth> Desproteger(string state);
}
