using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Planes;
using MiTurno.Application.Features.Admin.Planes.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Admin.Planes;

public class ActualizarPlanUseCaseTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ActualizarPlanUseCase _useCase;

    public ActualizarPlanUseCaseTests()
    {
        _useCase = new ActualizarPlanUseCase(new ActualizarPlanValidator(), _planRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanExistente_ActualizaSusDatos()
    {
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        _planRepository.GetByIdAsync(plan.Id).Returns(plan);

        var result = await _useCase.ExecuteAsync(
            plan.Id, new ActualizarPlanRequest("Básico Plus", 7000m, Periodicidad.Anual, 5, 500));

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Básico Plus");
        result.Value.Precio.Should().Be(7000m);
        result.Value.Periodicidad.Should().Be(Periodicidad.Anual);
        _planRepository.Received(1).Update(plan);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanInexistente_DevuelveFailure()
    {
        _planRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Plan?)null);

        var result = await _useCase.ExecuteAsync(
            Guid.NewGuid(), new ActualizarPlanRequest("Básico", 5000m, Periodicidad.Mensual, 3, 200));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Plan no encontrado.");
    }
}
