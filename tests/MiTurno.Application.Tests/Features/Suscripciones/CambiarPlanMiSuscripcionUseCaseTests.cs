using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Application.Features.Suscripciones.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class CambiarPlanMiSuscripcionUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway = Substitute.For<IPagoRecurrenteGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CambiarPlanMiSuscripcionUseCase _useCase;

    public CambiarPlanMiSuscripcionUseCaseTests()
    {
        var validarLimiteRecursosService = new ValidarLimiteRecursosService(_recursoRepository, _suscripcionRepository);
        _useCase = new CambiarPlanMiSuscripcionUseCase(
            _suscripcionRepository, _planRepository, validarLimiteRecursosService,
            _plataformaPagoConfiguracion, _pagoRecurrenteGateway, _unitOfWork);
        _recursoRepository.GetByNegocioIdAsync(Arg.Any<Guid>()).Returns(Array.Empty<Recurso>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanNuevoActivo_CambiaElPlanYDevuelveLaRespuestaActualizada()
    {
        var negocioId = Guid.NewGuid();
        var planViejo = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var planNuevo = Plan.Crear("Premium", 10000m, Periodicidad.Mensual, 10, 1000);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, planViejo);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _planRepository.GetByIdAsync(planNuevo.Id).Returns(planNuevo);

        var result = await _useCase.ExecuteAsync(negocioId, new CambiarPlanMiSuscripcionRequest(planNuevo.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.PlanId.Should().Be(planNuevo.Id);
        result.Value.PlanNombre.Should().Be("Premium");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConMasCanchasActivasQueElLimiteDelPlanNuevo_DevuelveFailureSinCambiarElPlan()
    {
        var negocioId = Guid.NewGuid();
        var planViejo = Plan.Crear("Premium", 10000m, Periodicidad.Mensual, 10, 1000);
        var planNuevo = Plan.Crear("Estándar", 5000m, Periodicidad.Mensual, limiteRecursos: 2, limiteReservasPorMes: 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, planViejo);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _planRepository.GetByIdAsync(planNuevo.Id).Returns(planNuevo);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([
            Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m),
            Recurso.Crear(negocioId, "Cancha 2", "Futbol", TimeSpan.FromMinutes(60), 5000m),
            Recurso.Crear(negocioId, "Cancha 3", "Futbol", TimeSpan.FromMinutes(60), 5000m),
        ]);

        var result = await _useCase.ExecuteAsync(negocioId, new CambiarPlanMiSuscripcionRequest(planNuevo.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Estándar");
        suscripcion.PlanId.Should().Be(planViejo.Id);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConCanchasActivasDentroDelLimiteDelPlanNuevo_CambiaElPlan()
    {
        var negocioId = Guid.NewGuid();
        var planViejo = Plan.Crear("Premium", 10000m, Periodicidad.Mensual, 10, 1000);
        var planNuevo = Plan.Crear("Estándar", 5000m, Periodicidad.Mensual, limiteRecursos: 3, limiteReservasPorMes: 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, planViejo);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _planRepository.GetByIdAsync(planNuevo.Id).Returns(planNuevo);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([
            Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m),
            Recurso.Crear(negocioId, "Cancha 2", "Futbol", TimeSpan.FromMinutes(60), 5000m),
            Recurso.Crear(negocioId, "Cancha 3", "Futbol", TimeSpan.FromMinutes(60), 5000m),
        ]);

        var result = await _useCase.ExecuteAsync(negocioId, new CambiarPlanMiSuscripcionRequest(planNuevo.Id));

        result.IsSuccess.Should().BeTrue();
        suscripcion.PlanId.Should().Be(planNuevo.Id);
    }

    [Fact]
    public async Task ExecuteAsync_SinSuscripcionAsignada_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns((Suscripcion?)null);

        var result = await _useCase.ExecuteAsync(negocioId, new CambiarPlanMiSuscripcionRequest(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanInexistente_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        var planViejo = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, planViejo);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _planRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Plan?)null);

        var result = await _useCase.ExecuteAsync(negocioId, new CambiarPlanMiSuscripcionRequest(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Plan no encontrado.");
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanInactivo_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        var planViejo = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, planViejo);
        var planInactivo = Plan.Crear("Descontinuado", 2000m, Periodicidad.Mensual, 1, 50);
        planInactivo.Desactivar();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _planRepository.GetByIdAsync(planInactivo.Id).Returns(planInactivo);

        var result = await _useCase.ExecuteAsync(negocioId, new CambiarPlanMiSuscripcionRequest(planInactivo.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Plan no encontrado.");
    }

    [Fact]
    public async Task ExecuteAsync_ConCobroAutomaticoActivo_ActualizaElMontoEnMercadoPago()
    {
        var negocioId = Guid.NewGuid();
        var planViejo = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var planNuevo = Plan.Crear("Premium", 10000m, Periodicidad.Mensual, 10, 1000);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, planViejo);
        suscripcion.AsignarPreapproval("preapproval-1");
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _planRepository.GetByIdAsync(planNuevo.Id).Returns(planNuevo);
        _plataformaPagoConfiguracion.AccessToken.Returns("PLATAFORMA-TOKEN");

        var result = await _useCase.ExecuteAsync(negocioId, new CambiarPlanMiSuscripcionRequest(planNuevo.Id));

        result.IsSuccess.Should().BeTrue();
        await _pagoRecurrenteGateway.Received(1).ActualizarMontoPreapprovalAsync(
            "PLATAFORMA-TOKEN", "preapproval-1", 10000m, Arg.Any<CancellationToken>());
    }
}
