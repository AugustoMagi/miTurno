using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiTurno.Application.Features.Suscripciones;

namespace MiTurno.Infrastructure.BackgroundJobs;

/// <summary>
/// Corre una vez por día y dispara <see cref="NotificarSuscripcionesPorVencerUseCase"/>. Usa un
/// scope nuevo en cada corrida porque el use case y sus repositorios son servicios "scoped", y este
/// servicio en sí es un singleton que vive durante toda la vida del proceso.
/// </summary>
public class SuscripcionVencimientoBackgroundService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromDays(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SuscripcionVencimientoBackgroundService> _logger;

    public SuscripcionVencimientoBackgroundService(
        IServiceScopeFactory scopeFactory, ILogger<SuscripcionVencimientoBackgroundService> logger)
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
            var useCase = scope.ServiceProvider.GetRequiredService<NotificarSuscripcionesPorVencerUseCase>();
            var resultado = await useCase.ExecuteAsync(diasAnticipacion: 3, cancellationToken);

            if (resultado.IsSuccess)
                _logger.LogInformation("Chequeo de suscripciones por vencer: {Cantidad} notificadas.", resultado.Value);
            else
                _logger.LogWarning("Chequeo de suscripciones por vencer falló: {Error}", resultado.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado corriendo el chequeo de suscripciones por vencer.");
        }
    }
}
