using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Application.Features.Suscripciones.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class ElegirPlanUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ElegirPlanUseCase _useCase;

    public ElegirPlanUseCaseTests()
    {
        _useCase = new ElegirPlanUseCase(_suscripcionRepository, _planRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_SinSuscripcionPrevia_CreaLaSuscripcionEnPruebaYDevuelveLaRespuesta()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns((Suscripcion?)null);
        _planRepository.GetByIdAsync(plan.Id).Returns(plan);

        var result = await _useCase.ExecuteAsync(negocioId, new ElegirPlanRequest(plan.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.PlanId.Should().Be(plan.Id);
        result.Value.PlanNombre.Should().Be("Básico");
        result.Value.Estado.Should().Be(EstadoSuscripcion.EnPrueba);
        result.Value.EstaActiva.Should().BeTrue();
        await _suscripcionRepository.Received(1).AddAsync(Arg.Any<Suscripcion>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConSuscripcionYaAsignada_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        var planViejo = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcionExistente = Suscripcion.IniciarPrueba(negocioId, planViejo);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcionExistente);

        var result = await _useCase.ExecuteAsync(negocioId, new ElegirPlanRequest(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Ya tenés una suscripción asignada.");
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanInexistente_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns((Suscripcion?)null);
        _planRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Plan?)null);

        var result = await _useCase.ExecuteAsync(negocioId, new ElegirPlanRequest(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Plan no encontrado.");
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanInactivo_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        var planInactivo = Plan.Crear("Descontinuado", 2000m, Periodicidad.Mensual, 1, 50);
        planInactivo.Desactivar();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns((Suscripcion?)null);
        _planRepository.GetByIdAsync(planInactivo.Id).Returns(planInactivo);

        var result = await _useCase.ExecuteAsync(negocioId, new ElegirPlanRequest(planInactivo.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Plan no encontrado.");
    }
}
