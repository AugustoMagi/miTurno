using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Planes;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Admin.Planes;

public class ListarPlanesUseCaseTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();

    private readonly ListarPlanesUseCase _useCase;

    public ListarPlanesUseCaseTests()
    {
        _useCase = new ListarPlanesUseCase(_planRepository);
    }

    [Fact]
    public async Task ExecuteAsync_DevuelveTodosLosPlanesIncluidosLosInactivos()
    {
        var activo = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var inactivo = Plan.Crear("Viejo", 3000m, Periodicidad.Mensual, 2, 100);
        inactivo.Desactivar();
        _planRepository.GetAllAsync().Returns([activo, inactivo]);

        var result = await _useCase.ExecuteAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Id == inactivo.Id && !p.Activo);
    }
}
