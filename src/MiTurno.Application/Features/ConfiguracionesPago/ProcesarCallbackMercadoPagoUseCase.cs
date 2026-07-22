using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.ConfiguracionesPago;

/// <summary>
/// Procesa la vuelta del flujo OAuth de Mercado Pago: descifra el "state" para saber a qué negocio
/// pertenece y recuperar el code_verifier de PKCE, canjea el "code" por los tokens reales, resuelve
/// el perfil del usuario conectado (para mostrar algo identificable en vez de un token), y reemplaza
/// la configuración de cobro activa del negocio por esta — mismo criterio de "una sola conexión
/// activa a la vez" que ConectarConfiguracionPagoUseCase.
/// </summary>
public class ProcesarCallbackMercadoPagoUseCase
{
    private readonly IEstadoOAuthProtector _estadoOAuthProtector;
    private readonly IMercadoPagoOAuthGateway _oauthGateway;
    private readonly IMercadoPagoOAuthConfiguracion _oauthConfiguracion;
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProcesarCallbackMercadoPagoUseCase(
        IEstadoOAuthProtector estadoOAuthProtector,
        IMercadoPagoOAuthGateway oauthGateway,
        IMercadoPagoOAuthConfiguracion oauthConfiguracion,
        IConfiguracionPagoRepository configuracionPagoRepository,
        IUnitOfWork unitOfWork)
    {
        _estadoOAuthProtector = estadoOAuthProtector;
        _oauthGateway = oauthGateway;
        _oauthConfiguracion = oauthConfiguracion;
        _configuracionPagoRepository = configuracionPagoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(
        string? code, string? state, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return Result.Failure("Faltan parámetros en la respuesta de Mercado Pago.");

        var estadoResult = _estadoOAuthProtector.Desproteger(state);
        if (estadoResult.IsFailure)
            return Result.Failure(estadoResult.Error!);
        var estado = estadoResult.Value;

        var tokenResult = await _oauthGateway.IntercambiarCodigoAsync(
            code, estado.CodeVerifier, _oauthConfiguracion.RedirectUri, cancellationToken);
        if (tokenResult.IsFailure)
            return Result.Failure(tokenResult.Error!);
        var token = tokenResult.Value;

        var usuarioResult = await _oauthGateway.ObtenerUsuarioAsync(token.AccessToken, cancellationToken);
        var alias = usuarioResult.IsSuccess && !string.IsNullOrWhiteSpace(usuarioResult.Value.Email)
            ? usuarioResult.Value.Email!
            : "Cuenta de Mercado Pago conectada";

        try
        {
            var activaAnterior = await _configuracionPagoRepository.GetActivaByNegocioIdAsync(estado.NegocioId, cancellationToken);
            if (activaAnterior is not null)
            {
                activaAnterior.Desconectar();
                _configuracionPagoRepository.Update(activaAnterior);
            }

            var expiraEn = DateTime.UtcNow.AddSeconds(token.ExpiraEnSegundos);
            var configuracion = ConfiguracionPago.ConectarConOAuth(
                estado.NegocioId, alias, token.AccessToken, token.RefreshToken, expiraEn);
            await _configuracionPagoRepository.AddAsync(configuracion, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
