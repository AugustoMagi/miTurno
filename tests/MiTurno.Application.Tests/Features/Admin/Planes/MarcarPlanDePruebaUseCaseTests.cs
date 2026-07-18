using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Planes;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Admin.Planes;

public class MarcarPlanDePruebaUseCaseTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly MarcarPlanDePruebaUseCase _useCase;

    public MarcarPlanDePruebaUseCaseTests()
    {
        _useCase = new MarcarPlanDePruebaUseCase(_planRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_SinPlanDePruebaPrevio_MarcaElNuevo()
    {
        var plan = Plan.Crear("Básico", 0m, Periodicidad.Mensual, 3, 200);
        _planRepository.GetByIdAsync(plan.Id).Returns(plan);
        _planRepository.GetPlanDePruebaAsync().Returns((Plan?)null);

        var result = await _useCase.ExecuteAsync(plan.Id);

        result.IsSuccess.Should().BeTrue();
        plan.EsPlanDePrueba.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConOtroPlanDePruebaPrevio_LoDesmarcaYMarcaElNuevo()
    {
        var planAnterior = Plan.Crear("Viejo", 0m, Periodicidad.Mensual, 1, 50);
        planAnterior.MarcarComoPlanDePrueba();
        var planNuevo = Plan.Crear("Nuevo", 0m, Periodicidad.Mensual, 3, 200);
        _planRepository.GetByIdAsync(planNuevo.Id).Returns(planNuevo);
        _planRepository.GetPlanDePruebaAsync().Returns(planAnterior);

        var result = await _useCase.ExecuteAsync(planNuevo.Id);

        result.IsSuccess.Should().BeTrue();
        planNuevo.EsPlanDePrueba.Should().BeTrue();
        planAnterior.EsPlanDePrueba.Should().BeFalse();
        _planRepository.Received(1).Update(planAnterior);
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanInexistente_DevuelveFailure()
    {
        _planRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Plan?)null);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Plan no encontrado.");
    }
}
