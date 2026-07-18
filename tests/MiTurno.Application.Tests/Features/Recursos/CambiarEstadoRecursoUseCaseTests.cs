using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Recursos;

public class CambiarEstadoRecursoUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CambiarEstadoRecursoUseCase _useCase;

    public CambiarEstadoRecursoUseCaseTests()
    {
        _useCase = new CambiarEstadoRecursoUseCase(_recursoRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConActivarFalse_DesactivaElRecurso()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id, activar: false);

        result.IsSuccess.Should().BeTrue();
        recurso.Activo.Should().BeFalse();
        _recursoRepository.Received(1).Update(recurso);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConActivarTrue_ReactivaElRecurso()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        recurso.Desactivar();
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id, activar: true);

        result.IsSuccess.Should().BeTrue();
        recurso.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailureSinModificar()
    {
        var recurso = Recurso.Crear(Guid.NewGuid(), "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), recurso.Id, activar: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
        recurso.Activo.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
