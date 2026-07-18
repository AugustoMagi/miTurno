using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Planes;
using MiTurno.Application.Features.Admin.Planes.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Admin.Planes;

public class CrearPlanUseCaseTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CrearPlanUseCase _useCase;

    public CrearPlanUseCaseTests()
    {
        _useCase = new CrearPlanUseCase(new CrearPlanValidator(), _planRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_CreaElPlan()
    {
        var request = new CrearPlanRequest("Básico", 5000m, Periodicidad.Mensual, 3, 200);

        var result = await _useCase.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Básico");
        result.Value.Precio.Should().Be(5000m);
        result.Value.EsPlanDePrueba.Should().BeFalse();
        await _planRepository.Received(1).AddAsync(Arg.Any<Plan>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPrecioNegativo_DevuelveFailureDeValidacionSinPersistir()
    {
        var request = new CrearPlanRequest("Básico", -1m, Periodicidad.Mensual, 3, 200);

        var result = await _useCase.ExecuteAsync(request);

        result.IsFailure.Should().BeTrue();
        await _planRepository.DidNotReceive().AddAsync(Arg.Any<Plan>(), Arg.Any<CancellationToken>());
    }
}
