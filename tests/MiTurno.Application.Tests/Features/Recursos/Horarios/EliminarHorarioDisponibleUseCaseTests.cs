using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos.Horarios;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Recursos.Horarios;

public class EliminarHorarioDisponibleUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly EliminarHorarioDisponibleUseCase _useCase;

    public EliminarHorarioDisponibleUseCaseTests()
    {
        _useCase = new EliminarHorarioDisponibleUseCase(_recursoRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConHorarioExistente_LoElimina()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var horario = HorarioDisponible.Crear(recurso.Id, DayOfWeek.Monday, TimeSpan.FromHours(18), TimeSpan.FromHours(22));
        recurso.AgregarHorarioDisponible(horario);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id, horario.Id);

        result.IsSuccess.Should().BeTrue();
        recurso.HorariosDisponibles.Should().BeEmpty();
        _recursoRepository.Received(1).Update(recurso);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConHorarioInexistente_DevuelveFailureDelDominio()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("El horario no existe para este recurso.");
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
