using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos.Horarios;
using MiTurno.Application.Features.Recursos.Horarios.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Recursos.Horarios;

public class AgregarHorarioDisponibleUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AgregarHorarioDisponibleUseCase _useCase;

    public AgregarHorarioDisponibleUseCaseTests()
    {
        _useCase = new AgregarHorarioDisponibleUseCase(
            new AgregarHorarioDisponibleValidator(), _recursoRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConHorarioValido_LoAgregaAlRecurso()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(
            negocioId, recurso.Id,
            new AgregarHorarioDisponibleRequest(DayOfWeek.Monday, TimeSpan.FromHours(18), TimeSpan.FromHours(22)));

        result.IsSuccess.Should().BeTrue();
        result.Value.DiaSemana.Should().Be(DayOfWeek.Monday);
        recurso.HorariosDisponibles.Should().ContainSingle();
        _recursoRepository.Received(1).Update(recurso);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConHoraInicioMayorOIgualAHoraFin_DevuelveFailureDeValidacion()
    {
        var result = await _useCase.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            new AgregarHorarioDisponibleRequest(DayOfWeek.Monday, TimeSpan.FromHours(22), TimeSpan.FromHours(18)));

        result.IsFailure.Should().BeTrue();
        await _recursoRepository.DidNotReceive().GetConHorariosYBloqueosAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConHorarioSuperpuestoAUnoExistente_DevuelveFailureDelDominio()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        recurso.AgregarHorarioDisponible(
            HorarioDisponible.Crear(recurso.Id, DayOfWeek.Monday, TimeSpan.FromHours(18), TimeSpan.FromHours(22)));
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(
            negocioId, recurso.Id,
            new AgregarHorarioDisponibleRequest(DayOfWeek.Monday, TimeSpan.FromHours(20), TimeSpan.FromHours(23)));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("El horario se superpone con uno ya existente para ese día.");
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailure()
    {
        var recurso = Recurso.Crear(Guid.NewGuid(), "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(
            Guid.NewGuid(), recurso.Id,
            new AgregarHorarioDisponibleRequest(DayOfWeek.Monday, TimeSpan.FromHours(18), TimeSpan.FromHours(22)));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
    }
}
