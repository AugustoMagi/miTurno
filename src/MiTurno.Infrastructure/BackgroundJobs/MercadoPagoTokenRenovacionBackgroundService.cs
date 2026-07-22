using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiTurno.Application.Features.ConfiguracionesPago;

namespace MiTurno.Infrastructure.BackgroundJobs;

/// <summary>
/// Corre cada una hora y renueva de antemano los AccessToken de Mercado Pago conectados por OAuth
/// que estén por vencer (duran unas horas, a diferencia del token manual que no vence), para que
/// CrearReservaUseCase y el webhook de pago siempre encuentren uno vigente sin tener que renovarlo
/// ellos mismos. Usa un scope nuevo en cada corrida por la misma razón que SuscripcionVencimientoBackgroundService.
/// </summary>
public class MercadoPagoTokenRenovacionBackgroundService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);
    private static readonly TimeSpan Anticipacion = TimeSpan.FromHours(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MercadoPagoTokenRenovacionBackgroundService> _logger;

    public MercadoPagoTokenRenovacionBackgroundService(
        IServiceScopeFactory scopeFactory, ILogger<MercadoPagoTokenRenovacionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            await EjecutarAsync(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<RenovarConexionesMercadoPagoUseCase>();
            var resultado = await useCase.ExecuteAsync(Anticipacion, cancellationToken);

            if (resultado.IsSuccess)
                _logger.LogInformation("Renovación de tokens de Mercado Pago: {Cantidad} renovados.", resultado.Value);
            else
                _logger.LogWarning("Renovación de tokens de Mercado Pago falló: {Error}", resultado.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado renovando tokens de Mercado Pago.");
        }
    }
}
