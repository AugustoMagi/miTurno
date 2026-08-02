using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class CambiarPlanConPagoUseCaseTests
{
    private const string WebhookBaseUrl = "https://miturno.test";

    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway = Substitute.For<IPagoRecurrenteGateway>();
    private readonly IFrontendConfiguracion _frontendConfiguracion = Substitute.For<IFrontendConfiguracion>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CambiarPlanConPagoUseCase _useCase;

    public CambiarPlanConPagoUseCaseTests()
    {
        _plataformaPagoConfiguracion.AccessToken.Returns("PLATAFORMA-TOKEN");
        _frontendConfiguracion.BaseUrl.Returns("https://panel.miturno.test");
        var validarLimiteRecursosService = new ValidarLimiteRecursosService(_recursoRepository, _suscripcionRepository);
        _useCase = new CambiarPlanConPagoUseCase(
            _suscripcionRepository, _planRepository, _negocioRepository, validarLimiteRecursosService,
            _plataformaPagoConfiguracion, _pagoRecurrenteGateway, _frontendConfiguracion, _unitOfWork);
    }

    private (Negocio negocio, Suscripcion suscripcion, Plan planActual, Plan planNuevo) EscenarioValido()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var planActual = Plan.Crear("Pro", 8000m, Periodicidad.Mensual, 5, 500);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, planActual);
        suscripcion.AsignarPreapproval("preapproval-pro-vigente");
        var planNuevo = Plan.Crear("Estándar", 5000m, Periodicidad.Mensual, 3, 200);

        _suscripcionRepository.GetByNegocioIdAsync(negocio.Id).Returns(suscripcion);
        _planRepository.GetByIdAsync(planNuevo.Id).Returns(planNuevo);
        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);
        _recursoRepository.GetByNegocioIdAsync(negocio.Id).Returns(new List<Recurso>());

        return (negocio, suscripcion, planActual, planNuevo);
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanValido_CreaLaPreapprovalPeroNoTocaElPlanNiLaVigente()
    {
        // Clave del fix: si el negocio no llega a pagar (cierra MP, vuelve atrás), no debe perder el
        // plan ni el cobro automático que ya tenía funcionando — por eso acá no se toca nada de eso.
        var (negocio, suscripcion, planActual, planNuevo) = EscenarioValido();
        _pagoRecurrenteGateway.CrearPreapprovalAsync(Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PreapprovalCreadoResult("preapproval-estandar-pendiente", "https://mp.test/pagar")));

        var result = await _useCase.ExecuteAsync(negocio.Id, planNuevo.Id, WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Value.InitPoint.Should().Be("https://mp.test/pagar");
        suscripcion.PlanId.Should().Be(planActual.Id);
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-pro-vigente");
        suscripcion.PlanPendienteId.Should().Be(planNuevo.Id);
        suscripcion.MercadoPagoPreapprovalIdPendiente.Should().Be("preapproval-estandar-pendiente");
        await _pagoRecurrenteGateway.DidNotReceive().CancelarPreapprovalAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _pagoRecurrenteGateway.Received(1).CrearPreapprovalAsync(
            Arg.Is<CrearPreapprovalRequest>(r => r!.FechaInicio == null && r.Monto == 5000m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConMasCanchasActivasQueElLimiteDelPlanNuevo_DevuelveFailureSinLlamarAlGateway()
    {
        var (negocio, _, _, planNuevo) = EscenarioValido();
        var duracion = TimeSpan.FromMinutes(60);
        _recursoRepository.GetByNegocioIdAsync(negocio.Id).Returns(new List<Recurso>
        {
            Recurso.Crear(negocio.Id, "Cancha 1", "Fútbol", duracion, 1000m),
            Recurso.Crear(negocio.Id, "Cancha 2", "Fútbol", duracion, 1000m),
            Recurso.Crear(negocio.Id, "Cancha 3", "Fútbol", duracion, 1000m),
            Recurso.Crear(negocio.Id, "Cancha 4", "Fútbol", duracion, 1000m),
        });

        var result = await _useCase.ExecuteAsync(negocio.Id, planNuevo.Id, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        await _pagoRecurrenteGateway.DidNotReceive().CrearPreapprovalAsync(
            Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanGratuito_DevuelveFailure()
    {
        var (negocio, _, _, _) = EscenarioValido();
        var planGratis = Plan.Crear("Gratis", 0m, Periodicidad.Mensual, 1, 50);
        _planRepository.GetByIdAsync(planGratis.Id).Returns(planGratis);

        var result = await _useCase.ExecuteAsync(negocio.Id, planGratis.Id, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        await _pagoRecurrenteGateway.DidNotReceive().CrearPreapprovalAsync(
            Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConElGatewayFallando_DevuelveFailureSinGuardarNada()
    {
        var (negocio, _, _, planNuevo) = EscenarioValido();
        _pagoRecurrenteGateway.CrearPreapprovalAsync(Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PreapprovalCreadoResult>("Mercado Pago no respondió."));

        var result = await _useCase.ExecuteAsync(negocio.Id, planNuevo.Id, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SinSuscripcionAsignada_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns((Suscripcion?)null);

        var result = await _useCase.ExecuteAsync(negocioId, Guid.NewGuid(), WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
    }
}
