using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Suscripciones;
using MiTurno.Application.Features.Admin.Suscripciones.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Admin.Suscripciones;

public class CambiarPlanSuscripcionUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway = Substitute.For<IPagoRecurrenteGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CambiarPlanSuscripcionUseCase _useCase;

    public CambiarPlanSuscripcionUseCaseTests()
    {
        _useCase = new CambiarPlanSuscripcionUseCase(
            _suscripcionRepository, _planRepository, _negocioRepository,
            _plataformaPagoConfiguracion, _pagoRecurrenteGateway, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanNuevoValido_CambiaElPlanYDevuelveLaRespuestaActualizada()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var planViejo = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var planNuevo = Plan.Crear("Premium", 10000m, Periodicidad.Mensual, 10, 1000);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, planViejo);

        _suscripcionRepository.GetByIdAsync(suscripcion.Id).Returns(suscripcion);
        _planRepository.GetByIdAsync(planNuevo.Id).Returns(planNuevo);
        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);

        var result = await _useCase.ExecuteAsync(suscripcion.Id, new CambiarPlanSuscripcionRequest(planNuevo.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.PlanId.Should().Be(planNuevo.Id);
        result.Value.PlanNombre.Should().Be("Premium");
        result.Value.PlanPrecio.Should().Be(10000m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConSuscripcionInexistente_DevuelveFailure()
    {
        _suscripcionRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Suscripcion?)null);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), new CambiarPlanSuscripcionRequest(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Suscripción no encontrada.");
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanNuevoInexistente_DevuelveFailure()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var planViejo = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, planViejo);
        _suscripcionRepository.GetByIdAsync(suscripcion.Id).Returns(suscripcion);
        _planRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Plan?)null);

        var result = await _useCase.ExecuteAsync(suscripcion.Id, new CambiarPlanSuscripcionRequest(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Plan no encontrado.");
    }
}
