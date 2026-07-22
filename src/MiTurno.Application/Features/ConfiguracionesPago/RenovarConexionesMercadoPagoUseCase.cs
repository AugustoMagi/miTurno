using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Application.Features.ConfiguracionesPago;

/// <summary>
/// Renueva de antemano el AccessToken de toda ConfiguracionPago conectada por OAuth que esté por
/// vencer, para que CrearReservaUseCase y el webhook de pago siempre encuentren un token vigente y
/// nunca tengan que lidiar con la renovación ellos mismos. Pensado para dispararse desde una tarea
/// programada, no desde un endpoint HTTP. Si Mercado Pago rechaza un refresh_token puntual (el
/// negocio revocó el acceso desde su cuenta), esa conexión queda con el AccessToken vencido —
/// CrearReservaUseCase la va a tratar como fallo de pasarela y degradar a mostrar el alias, no rompe
/// el resto de la corrida.
/// </summary>
public class RenovarConexionesMercadoPagoUseCase
{
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository;
    private readonly IMercadoPagoOAuthGateway _oauthGateway;
    private readonly IUnitOfWork _unitOfWork;

    public RenovarConexionesMercadoPagoUseCase(
        IConfiguracionPagoRepository configuracionPagoRepository,
        IMercadoPagoOAuthGateway oauthGateway,
        IUnitOfWork unitOfWork)
    {
        _configuracionPagoRepository = configuracionPagoRepository;
        _oauthGateway = oauthGateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> ExecuteAsync(TimeSpan anticipacion, CancellationToken cancellationToken = default)
    {
        var limite = DateTime.UtcNow.Add(anticipacion);
        var conexiones = await _configuracionPagoRepository.GetConexionesOAuthPorVencerAsync(limite, cancellationToken);

        var renovadas = 0;
        foreach (var configuracion in conexiones)
        {
            var refreshResult = await _oauthGateway.RefrescarTokenAsync(configuracion.RefreshToken!, cancellationToken);
            if (refreshResult.IsFailure)
                continue; // token revocado o Mercado Pago caído puntualmente: se reintenta en la próxima corrida

            var token = refreshResult.Value;
            configuracion.ActualizarTokensOAuth(token.AccessToken, token.RefreshToken, DateTime.UtcNow.AddSeconds(token.ExpiraEnSegundos));
            _configuracionPagoRepository.Update(configuracion);
            renovadas++;
        }

        if (renovadas > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(renovadas);
    }
}
