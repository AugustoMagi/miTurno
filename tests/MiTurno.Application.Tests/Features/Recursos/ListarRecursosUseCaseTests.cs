using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Recursos;

public class ListarRecursosUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();

    private readonly ListarRecursosUseCase _useCase;

    public ListarRecursosUseCaseTests()
    {
        _useCase = new ListarRecursosUseCase(_recursoRepository);
    }

    [Fact]
    public async Task ExecuteAsync_DevuelveTodosLosRecursosDelNegocio()
    {
        var negocioId = Guid.NewGuid();
        var recurso1 = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        var recurso2 = Recurso.Crear(negocioId, "Cancha 2", "Padel", TimeSpan.FromMinutes(90), 7000m);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([recurso1, recurso2]);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.Should().HaveCount(2);
        result.Select(r => r.Nombre).Should().Contain(["Cancha 1", "Cancha 2"]);
    }

    [Fact]
    public async Task ExecuteAsync_SinRecursos_DevuelveListaVacia()
    {
        var negocioId = Guid.NewGuid();
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([]);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.Should().BeEmpty();
    }
}
