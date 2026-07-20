using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Public;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Public;

public class ListarPlanesPublicosUseCaseTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();

    private readonly ListarPlanesPublicosUseCase _useCase;

    public ListarPlanesPublicosUseCaseTests()
    {
        _useCase = new ListarPlanesPublicosUseCase(_planRepository);
    }

    [Fact]
    public async Task ExecuteAsync_DevuelveSoloLosPlanesActivosOrdenadosPorPrecio()
    {
        var caro = Plan.Crear("Premium", 15000m, Periodicidad.Mensual, 10, 1000);
        var barato = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        _planRepository.GetActivosAsync().Returns([caro, barato]);

        var result = await _useCase.ExecuteAsync();

        result.Should().HaveCount(2);
        result[0].Nombre.Should().Be("Básico");
        result[1].Nombre.Should().Be("Premium");
    }
}
