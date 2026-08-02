using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class IniciarSuscripcionMercadoPagoUseCaseTests
{
    private const string WebhookBaseUrl = "https://miturno.test";

    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway = Substitute.For<IPagoRecurrenteGateway>();
    private readonly IFrontendConfiguracion _frontendConfiguracion = Substitute.For<IFrontendConfiguracion>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly IniciarSuscripcionMercadoPagoUseCase _useCase;

    public IniciarSuscripcionMercadoPagoUseCaseTests()
    {
        _plataformaPagoConfiguracion.AccessToken.Returns("PLATAFORMA-TOKEN");
        _frontendConfiguracion.BaseUrl.Returns("https://panel.miturno.test");
        _useCase = new IniciarSuscripcionMercadoPagoUseCase(
            _suscripcionRepository, _negocioRepository, _plataformaPagoConfiguracion,
            _pagoRecurrenteGateway, _frontendConfiguracion, _unitOfWork);
    }

    private (Negocio negocio, Suscripcion suscripcion) EscenarioValido()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, plan);
        _suscripcionRepository.GetByNegocioIdAsync(negocio.Id).Returns(suscripcion);
        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);
        return (negocio, suscripcion);
    }

    [Fact]
    public async Task ExecuteAsync_ConSuscripcionValida_CreaLaPreapprovalYGuardaSuId()
    {
        var (negocio, suscripcion) = EscenarioValido();
        _pagoRecurrenteGateway.CrearPreapprovalAsync(Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PreapprovalCreadoResult("preapproval-1", "https://mp.test/suscribirme/preapproval-1")));

        var result = await _useCase.ExecuteAsync(negocio.Id, WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Value.InitPoint.Should().Be("https://mp.test/suscribirme/preapproval-1");
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-1");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _pagoRecurrenteGateway.Received(1).CrearPreapprovalAsync(
            Arg.Is<CrearPreapprovalRequest>(r =>
                r!.AccessToken == "PLATAFORMA-TOKEN" &&
                r.PayerEmail == "negocio@test.com" &&
                r.Monto == 5000m &&
                r.BackUrl == "https://panel.miturno.test/panel/suscripcion?mp=vuelta" &&
                // No debe cobrar de inmediato: el primer cargo tiene que salir recién cuando termine
                // el período vigente (acá, el vencimiento de la prueba gratis), no el día que se autoriza.
                r.FechaInicio == suscripcion.FechaProximoVencimiento),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConCobrarInmediato_IgnoraElVencimientoVigenteYCobraYa()
    {
        // Cuando se acaba de cambiar de plan, el negocio decidió pagar el nuevo precio ya mismo, no
        // esperar a que termine el período del plan viejo.
        var (negocio, suscripcion) = EscenarioValido();
        _pagoRecurrenteGateway.CrearPreapprovalAsync(Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PreapprovalCreadoResult("preapproval-1", "https://mp.test/suscribirme/preapproval-1")));

        var antesDeEjecutar = DateTime.UtcNow;
        var result = await _useCase.ExecuteAsync(negocio.Id, WebhookBaseUrl, cobrarInmediato: true);

        result.IsSuccess.Should().BeTrue();
        await _pagoRecurrenteGateway.Received(1).CrearPreapprovalAsync(
            Arg.Is<CrearPreapprovalRequest>(r =>
                r!.FechaInicio != suscripcion.FechaProximoVencimiento &&
                r.FechaInicio >= antesDeEjecutar),
            Arg.Any<CancellationToken>());
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
    public async Task ExecuteAsync_ConPlanDePrecioCero_DevuelveFailureSinLlamarAlGateway()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var planGratuito = Plan.Crear("Prueba Gratis", 0m, Periodicidad.Mensual, 2, 100);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, planGratuito);
        _suscripcionRepository.GetByNegocioIdAsync(negocio.Id).Returns(suscripcion);

        var result = await _useCase.ExecuteAsync(negocio.Id, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        await _pagoRecurrenteGateway.DidNotReceive().CrearPreapprovalAsync(Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConCobroAutomaticoYaActivado_DevuelveFailure()
    {
        var (negocio, suscripcion) = EscenarioValido();
        suscripcion.AsignarPreapproval("preapproval-existente");
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-existente")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-existente", "authorized", null)));

        var result = await _useCase.ExecuteAsync(negocio.Id, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        await _pagoRecurrenteGateway.DidNotReceive().CrearPreapprovalAsync(Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConCobroAutomaticoActivoYCobrarInmediato_CancelaLaViejaYCreaUnaNueva()
    {
        // Viene de un cambio de plan: la Preapproval vieja quedó al precio del plan anterior, no
        // corresponde dejarla cobrando — se cancela y se autoriza una nueva a este plan.
        var (negocio, suscripcion) = EscenarioValido();
        suscripcion.AsignarPreapproval("preapproval-viejo");
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-viejo")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-viejo", "authorized", null)));
        _pagoRecurrenteGateway.CancelarPreapprovalAsync(Arg.Any<string>(), "preapproval-viejo")
            .Returns(Result.Success());
        _pagoRecurrenteGateway.CrearPreapprovalAsync(Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PreapprovalCreadoResult("preapproval-nuevo", "https://mp.test/suscribirme/preapproval-nuevo")));

        var result = await _useCase.ExecuteAsync(negocio.Id, WebhookBaseUrl, cobrarInmediato: true);

        result.IsSuccess.Should().BeTrue();
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-nuevo");
        await _pagoRecurrenteGateway.Received(1).CancelarPreapprovalAsync(
            Arg.Any<string>(), "preapproval-viejo", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConCobroAutomaticoActivoYCobrarInmediatoPeroFallaAlCancelar_DevuelveFailureSinCrearNada()
    {
        var (negocio, suscripcion) = EscenarioValido();
        suscripcion.AsignarPreapproval("preapproval-viejo");
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-viejo")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-viejo", "authorized", null)));
        _pagoRecurrenteGateway.CancelarPreapprovalAsync(Arg.Any<string>(), "preapproval-viejo")
            .Returns(Result.Failure("Mercado Pago no respondió."));

        var result = await _useCase.ExecuteAsync(negocio.Id, WebhookBaseUrl, cobrarInmediato: true);

        result.IsFailure.Should().BeTrue();
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-viejo");
        await _pagoRecurrenteGateway.DidNotReceive().CrearPreapprovalAsync(
            Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPreapprovalPrevioYaCanceladoEnMercadoPago_LoSueltaYCreaUnoNuevo()
    {
        var (negocio, suscripcion) = EscenarioValido();
        suscripcion.AsignarPreapproval("preapproval-viejo");
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-viejo")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-viejo", "cancelled", null)));
        _pagoRecurrenteGateway.CrearPreapprovalAsync(Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PreapprovalCreadoResult("preapproval-nuevo", "https://mp.test/suscribirme/preapproval-nuevo")));

        var result = await _useCase.ExecuteAsync(negocio.Id, WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-nuevo");
        await _pagoRecurrenteGateway.Received(1).CrearPreapprovalAsync(Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConElGatewayFallando_DevuelveFailureSinGuardarNada()
    {
        var (negocio, _) = EscenarioValido();
        _pagoRecurrenteGateway.CrearPreapprovalAsync(Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PreapprovalCreadoResult>("Mercado Pago no respondió."));

        var result = await _useCase.ExecuteAsync(negocio.Id, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
