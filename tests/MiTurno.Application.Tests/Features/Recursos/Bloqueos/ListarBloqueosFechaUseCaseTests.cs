using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos.Bloqueos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Recursos.Bloqueos;

public class ListarBloqueosFechaUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();

    private readonly ListarBloqueosFechaUseCase _useCase;

    public ListarBloqueosFechaUseCaseTests()
    {
        _useCase = new ListarBloqueosFechaUseCase(_recursoRepository);
    }

    [Fact]
    public async Task ExecuteAsync_DevuelveLosBloqueosOrdenadosPorFecha()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var fechaLejana = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(20);
        var fechaCercana = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);
        recurso.AgregarBloqueoFecha(BloqueoFecha.Crear(recurso.Id, fechaLejana));
        recurso.AgregarBloqueoFecha(BloqueoFecha.Crear(recurso.Id, fechaCercana));
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Fecha.Should().Be(fechaCercana);
        result.Value[1].Fecha.Should().Be(fechaLejana);
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
