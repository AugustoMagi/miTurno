using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos.Bloqueos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Recursos.Bloqueos;

public class EliminarBloqueoFechaUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly EliminarBloqueoFechaUseCase _useCase;

    public EliminarBloqueoFechaUseCaseTests()
    {
        _useCase = new EliminarBloqueoFechaUseCase(_recursoRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConBloqueoExistente_LoElimina()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var bloqueo = BloqueoFecha.Crear(recurso.Id, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7));
        recurso.AgregarBloqueoFecha(bloqueo);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id, bloqueo.Id);

        result.IsSuccess.Should().BeTrue();
        recurso.BloqueosFecha.Should().BeEmpty();
        _recursoRepository.Received(1).Update(recurso);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConBloqueoInexistente_DevuelveFailureDelDominio()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("El bloqueo no existe para este recurso.");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailure()
    {
        var recurso = Recurso.Crear(Guid.NewGuid(), "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), recurso.Id, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
    }
}
