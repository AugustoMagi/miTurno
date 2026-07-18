using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos.Horarios;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Recursos.Horarios;

public class ListarHorariosDisponiblesUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();

    private readonly ListarHorariosDisponiblesUseCase _useCase;

    public ListarHorariosDisponiblesUseCaseTests()
    {
        _useCase = new ListarHorariosDisponiblesUseCase(_recursoRepository);
    }

    [Fact]
    public async Task ExecuteAsync_DevuelveTodosLosHorariosDelRecurso()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        recurso.AgregarHorarioDisponible(
            HorarioDisponible.Crear(recurso.Id, DayOfWeek.Monday, TimeSpan.FromHours(18), TimeSpan.FromHours(22)));
        recurso.AgregarHorarioDisponible(
            HorarioDisponible.Crear(recurso.Id, DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(12)));
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailure()
    {
        var recurso = Recurso.Crear(Guid.NewGuid(), "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), recurso.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
    }
}
