using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Planes;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Admin.Planes;

public class DesactivarPlanUseCaseTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DesactivarPlanUseCase _useCase;

    public DesactivarPlanUseCaseTests()
    {
        _useCase = new DesactivarPlanUseCase(_planRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanExistente_LoDesactiva()
    {
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        _planRepository.GetByIdAsync(plan.Id).Returns(plan);

        var result = await _useCase.ExecuteAsync(plan.Id);

        result.IsSuccess.Should().BeTrue();
        plan.Activo.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
