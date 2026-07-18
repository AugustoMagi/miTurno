using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Recursos;

public class ObtenerRecursoUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();

    private readonly ObtenerRecursoUseCase _useCase;

    public ObtenerRecursoUseCaseTests()
    {
        _useCase = new ObtenerRecursoUseCase(_recursoRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDelNegocio_DevuelveSusDatos()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Cancha 1");
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailureComoSiNoExistiera()
    {
        var recurso = Recurso.Crear(Guid.NewGuid(), "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), recurso.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoInexistente_DevuelveFailure()
    {
        _recursoRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Recurso?)null);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
    }
}
