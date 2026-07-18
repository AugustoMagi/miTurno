using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class GenerarPagoSuscripcionUseCaseTests
{
    private const string WebhookBaseUrl = "https://miturno.test";

    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoGateway _pagoGateway = Substitute.For<IPagoGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GenerarPagoSuscripcionUseCase _useCase;

    public GenerarPagoSuscripcionUseCaseTests()
    {
        _plataformaPagoConfiguracion.AccessToken.Returns("PLATAFORMA-TOKEN");
        _useCase = new GenerarPagoSuscripcionUseCase(
            _suscripcionRepository, _plataformaPagoConfiguracion, _pagoGateway, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_SinPagoPendientePrevio_CreaUnoNuevoYDevuelveElLinkPago()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoGateway.CrearPreferenciaAsync(Arg.Any<CrearPreferenciaPagoRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PreferenciaPagoResult("pref-1", "https://mp.test/pagar/pref-1")));

        var result = await _useCase.ExecuteAsync(negocioId, WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Value.LinkPago.Should().Be("https://mp.test/pagar/pref-1");
        result.Value.Monto.Should().Be(5000m);
        suscripcion.Pagos.Should().HaveCount(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _pagoGateway.Received(1).CrearPreferenciaAsync(
            Arg.Is<CrearPreferenciaPagoRequest>(r => r!.AccessToken == "PLATAFORMA-TOKEN"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPagoPendientePrevio_ReusaElMismoPagoSinCrearOtro()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        var pagoExistente = PagoSuscripcion.Registrar(suscripcion.Id, plan.Precio, null);
        suscripcion.RegistrarPago(pagoExistente);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoGateway.CrearPreferenciaAsync(Arg.Any<CrearPreferenciaPagoRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PreferenciaPagoResult("pref-1", "https://mp.test/pagar/pref-1")));

        var result = await _useCase.ExecuteAsync(negocioId, WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(pagoExistente.Id);
        suscripcion.Pagos.Should().HaveCount(1);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SinSuscripcionAsignada_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns((Suscripcion?)null);

        var result = await _useCase.ExecuteAsync(negocioId, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanDePrecioCero_DevuelveFailureSinCrashear()
    {
        var negocioId = Guid.NewGuid();
        var planGratuito = Plan.Crear("Prueba Gratis", 0m, Periodicidad.Mensual, 2, 100);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, planGratuito);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);

        var result = await _useCase.ExecuteAsync(negocioId, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        await _pagoGateway.DidNotReceive().CrearPreferenciaAsync(Arg.Any<CrearPreferenciaPagoRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConElGatewayFallando_DevuelveElPagoConLinkPagoNulo()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoGateway.CrearPreferenciaAsync(Arg.Any<CrearPreferenciaPagoRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PreferenciaPagoResult>("Mercado Pago no respondió."));

        var result = await _useCase.ExecuteAsync(negocioId, WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Value.LinkPago.Should().BeNull();
    }
}
