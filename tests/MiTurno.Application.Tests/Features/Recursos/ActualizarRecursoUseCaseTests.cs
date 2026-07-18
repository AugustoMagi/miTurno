using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos;
using MiTurno.Application.Features.Recursos.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Recursos;

public class ActualizarRecursoUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ActualizarRecursoUseCase _useCase;

    public ActualizarRecursoUseCaseTests()
    {
        _useCase = new ActualizarRecursoUseCase(new ActualizarRecursoValidator(), _recursoRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDelNegocio_ActualizaSusDatos()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha Vieja", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(
            negocioId, recurso.Id, new ActualizarRecursoRequest("Cancha Nueva", "Padel", 90, 7000m));

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Cancha Nueva");
        result.Value.DuracionTurnoMinutos.Should().Be(90);
        _recursoRepository.Received(1).Update(recurso);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailureSinActualizar()
    {
        var recurso = Recurso.Crear(Guid.NewGuid(), "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(
            Guid.NewGuid(), recurso.Id, new ActualizarRecursoRequest("Nuevo Nombre", "Padel", 90, 7000m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoInexistente_DevuelveFailure()
    {
        _recursoRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Recurso?)null);

        var result = await _useCase.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), new ActualizarRecursoRequest("Nombre", "Tipo", 60, 5000m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
    }

    [Fact]
    public async Task ExecuteAsync_ConNombreVacio_DevuelveFailureDeValidacionSinConsultarRepositorio()
    {
        var result = await _useCase.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), new ActualizarRecursoRequest("", "Tipo", 60, 5000m));

        result.IsFailure.Should().BeTrue();
        await _recursoRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
